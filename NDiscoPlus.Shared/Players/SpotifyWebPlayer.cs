using Microsoft.Extensions.Logging;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NDiscoPlus.Shared.Players;

record PlayingContext
{
    public PlayingContext(DateTimeOffset fetchTimestamp, CurrentlyPlayingContext? context, FullTrack? track)
    {
        if (context is null != track is null)
            throw new ArgumentException("Context and track have to both be equally null.");

        FetchTimestamp = fetchTimestamp;
        Context = context;
        Track = track;
    }

    public DateTimeOffset FetchTimestamp { get; }

    [NotNullIfNotNull(nameof(Track))]
    public CurrentlyPlayingContext? Context { get; }

    [NotNullIfNotNull(nameof(Context))]
    public FullTrack? Track { get; }

    public TimeSpan ComputeCurrentProgress(DateTimeOffset nowUtc)
    {
        return TimeSpan.FromMilliseconds(Context!.ProgressMs) + (nowUtc - FetchTimestamp);
    }
}

public class SpotifyWebPlayer : SpotifyPlayer
{
    const int pollRate = 5; // how many seconds there should be between polls (very coarse; elapsed time is computed very inaccurately)

    const int contextWindowSize = 45 / 5; // How many polls we can fit in 45 seconds.
    const int eliminateExtremesWhen = 5; // eliminate extremes when count is more or equal than

    const double seekToleranceSeconds = 5;

    // At which positions to refresh the next track at the end. Set as empty to disable.
    static readonly TimeSpan[] refreshNextTrackFromEnd = [TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)];

    static readonly TimeSpan NextSongTolerance = TimeSpan.FromMilliseconds(100); // Add a bit of tolerance to make sure we don't spam Spotify with requests

    private readonly SpotifyClient client;

    readonly Queue<PlayingContext> contexts = new(capacity: contextWindowSize);
    Task? playerFetch = null;

    readonly Task<FullTrack?>?[] nextTrackFetches = Enumerable.Repeat<Task<FullTrack?>?>(null, refreshNextTrackFromEnd.Length).ToArray();

    TimeSpan? previousProgress;

    bool canRefreshNextTrackOnTrackEnd = true;

    readonly ILogger<SpotifyWebPlayer>? logger;

    public SpotifyWebPlayer(SpotifyClient client, ILogger<SpotifyWebPlayer>? logger = null)
    {
        this.client = client;
        this.logger = logger;
    }

    private async Task _UnrestrictedFetchPlayer()
    {
        logger?.LogInformation("Fetching new playing context...");
        CurrentlyPlayingContext? playContext = await client.Player.GetCurrentPlayback();
        HandleUpdate(playContext);
    }

    private async Task<FullTrack?> UnrestrictedFetchNextTrack()
    {
        logger?.LogInformation("Fetching next track context...");
        QueueResponse queue = await client.Player.GetQueue();
        return queue.Queue.OfType<FullTrack>().FirstOrDefault();
    }

    /// <summary>
    /// Returns null if there is a fetch already running.
    /// </summary>
    private Task FetchPlayer()
    {
        if (playerFetch is null || playerFetch.IsCompleted)
            playerFetch = _UnrestrictedFetchPlayer();
        return playerFetch;
    }

    private void HandleUpdate(CurrentlyPlayingContext? playContext)
    {
        DateTimeOffset time = DateTimeOffset.UtcNow;

        FullTrack? track = playContext?.Item as FullTrack;
        bool trackChanged = false;

        lock (contexts)
        {
            if (contexts.TryPeek(out var c))
            {
                if (track is null)
                {
                    contexts.Clear();
                }
                else if (c.Track?.Id != track.Id)
                {
                    contexts.Clear();
                    trackChanged = true;
                }
            }

            if (!(playContext?.IsPlaying ?? false))
                contexts.Clear();

            PlayingContext? lastContext = contexts.LastOrDefault();
            if (playContext is not null && lastContext is not null)
            {
                TimeSpan diff = TimeSpan.FromMilliseconds(playContext.ProgressMs) - lastContext.ComputeCurrentProgress(time);
                // If the difference between the last context's extrapolated time and the current context differ too much, we most likely seeked.
                if (Math.Abs(diff.TotalSeconds) > seekToleranceSeconds)
                {
                    logger?.LogInformation("Seek detected. Clearing contexts...");
                    contexts.Clear();
                }
            }

            while (contexts.Count >= contextWindowSize)
                contexts.Dequeue();

            PlayingContext ctx = new(time, playContext, track);
            contexts.Enqueue(ctx);
        }

        if (trackChanged)
            Array.Fill(nextTrackFetches, null);
    }

    protected override async Task Init()
    {
        await FetchPlayer();
    }

    protected override SpotifyPlayerContext? Update()
    {
        PlayingContext[] contexts;
        lock (this.contexts)
        {
            contexts = this.contexts.ToArray();
        }

        PlayingContext lastContext = contexts[^1];
        TimeSpan ahead = DateTimeOffset.UtcNow - lastContext.FetchTimestamp;
        if (ahead.TotalSeconds > pollRate)
            FetchPlayer();

        if (contexts.Length < 1)
            return null;

        if (lastContext.Context is null)
        {
            Debug.Assert(lastContext.Track is null);
            Debug.Assert(contexts.Length == 1);
            return null;
        }

        Debug.Assert(lastContext.Track is not null);


        TimeSpan progress;
        if (lastContext.Context.IsPlaying)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            TimeSpan[] progresses = contexts.Where(ctx => ctx.Context?.IsPlaying ?? false).Select(ctx => ctx.ComputeCurrentProgress(now)).ToArray();
            if (contexts.Length >= eliminateExtremesWhen)
                progresses = progresses.Order().Skip(1).SkipLast(1).ToArray();

            TimeSpan acc = TimeSpan.Zero;
            foreach (TimeSpan p in progresses)
                acc += p;
            progress = acc / progresses.Length;
        }
        else
        {
            progress = TimeSpan.FromMilliseconds(lastContext.Context.ProgressMs);
        }

        TimeSpan trackDuration = TimeSpan.FromMilliseconds(lastContext.Track.DurationMs);

        for (int i = 0; i < refreshNextTrackFromEnd.Length; i++)
        {
#if DEBUG
            if (i > 0)
                Debug.Assert(refreshNextTrackFromEnd[i - 1] > refreshNextTrackFromEnd[i]);
#endif
            TimeSpan left = trackDuration - progress;
            if (left <= refreshNextTrackFromEnd[i])
            {
                if (nextTrackFetches[i] is null)
                    nextTrackFetches[i] = UnrestrictedFetchNextTrack();
            }
            else
            {
                break;
            }
        }

        if ((progress - TimeSpan.FromMilliseconds(lastContext.Track.DurationMs)) > NextSongTolerance)
        {
            if (canRefreshNextTrackOnTrackEnd)
            {
                FetchPlayer();
                canRefreshNextTrackOnTrackEnd = false;
            }
        }
        else
        {
            canRefreshNextTrackOnTrackEnd = true;
        }

        // Do not allow progress to jump backwards due to clock sync changes
        // We use max tolerance of 1 second so that if the user adjust the music position manually, we don't bug out.
        if (previousProgress is TimeSpan pp && progress < pp && Math.Abs((progress - pp).TotalSeconds) < 1)
            progress = pp;
        previousProgress = progress;

        FullTrack? nextTrack = null;
        for (int i = (nextTrackFetches.Length - 1); i >= 0; i--)
        {
            Task<FullTrack?>? nextTrackFetch = nextTrackFetches[i];
            if (nextTrackFetch?.IsCompleted == true)
                nextTrack = nextTrackFetch.Result;
        }

        return new SpotifyPlayerContext(
            progress: progress,
            isPlaying: lastContext.Context.IsPlaying,
            track: SpotifyPlayerTrack.FromSpotifyTrack(lastContext.Track),
            nextTrack: SpotifyPlayerTrack.FromSpotifyTrackOrNull(nextTrack)
        );
    }
}
