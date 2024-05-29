using Microsoft.Extensions.Logging;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NDiscoPlus.Shared.Players;

record ClockDelta(TimeSpan Delta1, TimeSpan Delta2, TimeSpan Latency)
{
    public IEnumerable<TimeSpan> ComputeClockDeltas()
    {
        TimeSpan halfLatency = Latency / 2;

        yield return Delta1 + halfLatency;
        yield return Delta2 + halfLatency;
    }
}

record PlayingContext(DateTimeOffset FetchTimestamp, [NotNullIfNotNull(nameof(Track))] CurrentlyPlayingContext? Context, [NotNullIfNotNull(nameof(Context))] FullTrack? Track);

public class SpotifyWebPlayer : SpotifyPlayer
{
    const int pollRate = 5; // how many seconds there should be between polls (very coarse; elapsed time is computed very inaccurately)
    const int contextWindowSize = 20 / 5; // How many polls we can fit in 20 seconds.
    static readonly TimeSpan NextSongTolerance = TimeSpan.FromMilliseconds(100); // Add a bit of tolerance to make sure we don't spam Spotify with requests

    private readonly SpotifyClient client;

    readonly object contextLock = new();
    bool isFetchingContext = false;
    readonly Queue<PlayingContext> contexts = new(capacity: contextWindowSize);

    TimeSpan? previousProgress;

    readonly ILogger<SpotifyWebPlayer>? logger;

    public SpotifyWebPlayer(SpotifyClient client, ILogger<SpotifyWebPlayer>? logger = null)
    {
        this.client = client;
        this.logger = logger;
    }

    private async Task Fetch()
    {
        logger?.LogInformation("Fetching new playing context...");

        lock (contextLock)
        {
            isFetchingContext = true;
        }

        CurrentlyPlayingContext? playContext = await client.Player.GetCurrentPlayback();
        DateTimeOffset time = DateTimeOffset.UtcNow;

        FullTrack? track = playContext?.Item as FullTrack;

        lock (contextLock)
        {
            if (contexts.TryPeek(out var c) && (track is null || c.Track?.Id != track.Id))
                contexts.Clear();

            // if less or equal than previous context 5 seconds ago, we most likely seeked backwards, if one of the two or both are null, returns false
            if (contexts.LastOrDefault()?.Context?.ProgressMs < playContext?.ProgressMs)
                contexts.Clear();

            while (contexts.Count >= contextWindowSize)
                contexts.Dequeue();

            PlayingContext ctx = new(time, playContext, track);
            contexts.Enqueue(ctx);
            isFetchingContext = false;
        }
    }

    protected override async Task Init()
    {
        await Fetch();
    }

    protected override SpotifyPlayerContext? GetContext()
    {
        PlayingContext[] contexts;
        bool isFetchingContext;
        lock (contextLock)
        {
            contexts = this.contexts.ToArray();
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

        TimeSpan progress;
        if (lastContext.Context.IsPlaying)
        {
            TimeSpan acc = TimeSpan.Zero;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (PlayingContext ctx in contexts)
                acc += TimeSpan.FromMilliseconds(ctx.Context!.ProgressMs) + (now - ctx.FetchTimestamp);

            progress = acc / contexts.Length;
        }
        else
        {
            progress = TimeSpan.FromMilliseconds(lastContext.Context.ProgressMs);
        }


        if (progress - TimeSpan.FromMilliseconds(lastContext.Track!.DurationMs) > NextSongTolerance && !isFetchingContext)
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
            lastContext.Context!.IsPlaying,
            lastContext.Track!.Name,
            TimeSpan.FromMilliseconds(lastContext.Track.DurationMs),
            lastContext.Track.Album.Images[0].Url,
            lastContext.Track.Artists.Select(a => a.Name).ToArray()
        );
    }
}
