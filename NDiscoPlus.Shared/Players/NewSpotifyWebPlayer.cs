using Microsoft.Extensions.Logging;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NDiscoPlus.Shared.Players;

public class NewSpotifyWebPlayer : SpotifyPlayer
{
    const int pollRate = 5; // how many seconds there should be between polls (very coarse; elapsed time is computed very inaccurately)
    const int contextWindowSize = 35 / 5; // How many polls we can fit in 35 seconds.
    const double seekToleranceSeconds = 5;
    static readonly TimeSpan? refreshNextTrackFromEnd = TimeSpan.FromSeconds(10); // How long before the track end should we refresh the next track (null to disable)

    static readonly TimeSpan NextSongTolerance = TimeSpan.FromMilliseconds(100); // Add a bit of tolerance to make sure we don't spam Spotify with requests

    private readonly SpotifyClient client;

    readonly Queue<PlayingContext> contexts = new(capacity: contextWindowSize);
    Task? playerFetch = null;

    FullTrack? nextTrack = null;
    Task? nextTrackFetch = null;

    TimeSpan? previousProgress;

    readonly ILogger<NewSpotifyWebPlayer>? logger;

    public NewSpotifyWebPlayer(SpotifyClient client, ILogger<NewSpotifyWebPlayer>? logger = null)
    {
        this.client = client;
        this.logger = logger;
    }

    private async Task __FetchPlayer()
    {
        logger?.LogInformation("Fetching new playing context...");
        CurrentlyPlayingContext? playContext = await client.Player.GetCurrentPlayback();
        HandleUpdate(playContext);
    }

    private async Task __FetchNextTrack()
    {
        QueueResponse queue = await client.Player.GetQueue();
        nextTrack = queue.Queue.OfType<FullTrack>().FirstOrDefault();
    }

    /// <summary>
    /// Returns null if there is a fetch already running.
    /// </summary>
    private Task FetchPlayer()
    {
        if (playerFetch is null || playerFetch.IsCompleted)
            playerFetch = __FetchPlayer();
        return playerFetch;
    }

    /// <summary>
    /// Returns null if there is a fetch already running.
    /// </summary>
    private Task FetchNextTrack()
    {
        if (nextTrackFetch is null || nextTrackFetch.IsCompleted)
            nextTrackFetch = __FetchNextTrack();
        return nextTrackFetch;
    }

    private void HandleUpdate(CurrentlyPlayingContext? playContext)
    {
        DateTimeOffset time = DateTimeOffset.UtcNow;

        FullTrack? track = playContext?.Item as FullTrack;
        bool trackChanged = false;

        lock (contexts)
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
        {
            nextTrack = null;
            _ = FetchNextTrack();
        }
    }

    protected override async Task Init()
    {
        await FetchPlayer();
        await FetchNextTrack();
    }

    protected override SpotifyPlayerContext? Update()
    {
        PlayingContext[] contexts;
        lock (this.contexts)
        {
            contexts = this.contexts.ToArray();
        }
        FullTrack? nextTrack = this.nextTrack;

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

        TimeSpan trackDuration = TimeSpan.FromMilliseconds(lastContext.Track.DurationMs);
        if (refreshNextTrackFromEnd is TimeSpan rntfe && (trackDuration - progress) <= rntfe)
            FetchNextTrack();

        if (progress - TimeSpan.FromMilliseconds(lastContext.Track.DurationMs) > NextSongTolerance)
            FetchPlayer();

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
