using Microsoft.Extensions.Logging;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NDiscoPlus.Shared.Players;

record PlayingContext(
    DateTimeOffset FetchTimestamp,
    [NotNullIfNotNull(nameof(Track))] CurrentlyPlayingContext? Context,
    [NotNullIfNotNull(nameof(Context))] FullTrack? Track
)
{
    public TimeSpan ComputeCurrentProgress(DateTimeOffset nowUtc)
    {
        return TimeSpan.FromMilliseconds(Context!.ProgressMs) + (nowUtc - FetchTimestamp);
    }
}

public class OldSpotifyWebPlayer : SpotifyPlayer
{
    const int pollRate = 5; // how many seconds there should be between polls (very coarse; elapsed time is computed very inaccurately)
    const int contextWindowSize = 35 / 5; // How many polls we can fit in 35 seconds.
    static readonly TimeSpan NextSongTolerance = TimeSpan.FromMilliseconds(100); // Add a bit of tolerance to make sure we don't spam Spotify with requests

    private readonly SpotifyClient client;

    readonly object contextLock = new();
    bool isFetchingContext = false;
    FullTrack? nextTrackContext = null;
    readonly Queue<PlayingContext> contexts = new(capacity: contextWindowSize);

    TimeSpan? previousProgress;

    readonly ILogger<OldSpotifyWebPlayer>? logger;

    public OldSpotifyWebPlayer(SpotifyClient client, ILogger<OldSpotifyWebPlayer>? logger = null)
    {
        this.client = client;
        this.logger = logger;
    }

    private Task Fetch() => Fetch(false);
    private async Task Fetch(bool isFirstFetch)
    {
        logger?.LogInformation("Fetching new playing context...");

        lock (contextLock)
        {
            isFetchingContext = true;
        }

        CurrentlyPlayingContext? playContext = await client.Player.GetCurrentPlayback();
        bool trackChanged = HandleUpdate(playContext);

        FullTrack? nextTrack = null;
        if (trackChanged || isFirstFetch)
            nextTrack = await FetchNextTrack();

        lock (contextLock)
        {
            if (nextTrack is not null)
                nextTrackContext = nextTrack;
            isFetchingContext = false;
        }
    }

    private async Task<FullTrack?> FetchNextTrack()
    {
        QueueResponse queue = await client.Player.GetQueue();
        return queue.Queue.OfType<FullTrack>().FirstOrDefault();
    }

    private bool HandleUpdate(CurrentlyPlayingContext? playContext)
    {
        DateTimeOffset time = DateTimeOffset.UtcNow;

        FullTrack? track = playContext?.Item as FullTrack;
        bool trackChanged = false;

        lock (contextLock)
        {
            if (contexts.TryPeek(out var c) && (track is null || c.Track?.Id != track.Id))
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

            PlayingContext? lastContext = contexts.LastOrDefault();
            if (playContext is not null && lastContext is not null)
            {
                TimeSpan diff = TimeSpan.FromMilliseconds(playContext.ProgressMs) - lastContext.ComputeCurrentProgress(time);
                // If the difference between the last context's extrapolated time and the current context differ too much, we most likely seeked.
                if (Math.Abs(diff.TotalSeconds) > 5)
                {
                    logger?.LogInformation("Seek detected. Clearing contexts...");
                    contexts.Clear();
                }
            }

            while (contexts.Count >= contextWindowSize)
                contexts.Dequeue();

            PlayingContext ctx = new(time, playContext, track);
            contexts.Enqueue(ctx);
            if (trackChanged)
                nextTrackContext = null;
        }

        return trackChanged;
    }

    protected override async Task Init()
    {
        await Fetch(isFirstFetch: true);
    }

    protected override SpotifyPlayerContext? Update()
    {
        PlayingContext[] contexts;
        FullTrack? nextTrack;
        bool isFetchingContext;
        lock (contextLock)
        {
            contexts = this.contexts.ToArray();
            nextTrack = nextTrackContext;
            isFetchingContext = this.isFetchingContext;
        }

        PlayingContext lastContext = contexts[^1];
        TimeSpan ahead = DateTimeOffset.UtcNow - lastContext.FetchTimestamp;
        if (ahead.TotalSeconds > pollRate && !isFetchingContext)
        {
            Task.Run(Fetch);
            isFetchingContext = true;
        }

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
            TimeSpan acc = TimeSpan.Zero;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (PlayingContext ctx in contexts)
                acc += ctx.ComputeCurrentProgress(now);

            progress = acc / contexts.Length;
        }
        else
        {
            progress = TimeSpan.FromMilliseconds(lastContext.Context.ProgressMs);
        }


        if (progress - TimeSpan.FromMilliseconds(lastContext.Track.DurationMs) > NextSongTolerance && !isFetchingContext)
        {
            Task.Run(Fetch);
        }

        // Do not allow progress to jump backwards due to clock sync changes
        // We use max tolerance of 1 second so that if the user adjust the music position manually, we don't bug out.
        if (previousProgress is TimeSpan pp && progress < pp && Math.Abs((progress - pp).TotalSeconds) < 1)
            progress = pp;
        previousProgress = progress;

        return new SpotifyPlayerContext(
            progress,
            lastContext.Context.IsPlaying,
            SpotifyPlayerTrack.FromSpotifyTrack(lastContext.Track),
            nextTrack is not null ? SpotifyPlayerTrack.FromSpotifyTrack(nextTrack) : null
        );
    }
}
