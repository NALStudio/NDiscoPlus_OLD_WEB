using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System.Diagnostics;

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

record LatestPlayingContext(DateTimeOffset FetchedOn, CurrentlyPlayingContext? Context);

public class SpotifyWebPlayer : SpotifyPlayer
{
    const int ClockDeltaQueueMaxSize = 64;
    const int pollRate = 5; // how many seconds there should be between polls (very coarse; elapsed time is computed very inaccurately)

    private readonly SpotifyClient client;

    private LatestPlayingContext? latestContext;
    private readonly Queue<ClockDelta> clockDeltas = new(ClockDeltaQueueMaxSize);

    TimeSpan? clockDelta;
    TimeSpan? previousProgress;

    public SpotifyWebPlayer(SpotifyClient client)
    {
        this.client = client;
    }

    // Modified version of: https://gamedev.stackexchange.com/questions/687/game-clock-synchronization-in-python/691#691
    private async Task Fetch()
    {
        // local timestamp before request
        DateTimeOffset t1 = DateTimeOffset.UtcNow;

        CurrentlyPlayingContext? playContext = await client.Player.GetCurrentPlayback();
        if (playContext is null)
        {
            latestContext = new LatestPlayingContext(DateTimeOffset.UtcNow, playContext);
            return;
        }

        // local timestamp after request
        DateTimeOffset t2 = DateTimeOffset.UtcNow;


        // server timestamp
        DateTimeOffset serverTs = DateTimeOffset.FromUnixTimeMilliseconds(playContext.Timestamp);

        TimeSpan latency = t2 - t1;

        // here is where we deviate from the reference
        // we compute both deltas and store them with the latency
        // the deltas with the smallest latency should be the most accurate (https most likely didn't retry)
        // we then compute deltas by averaging all known deltas

        TimeSpan csDelta1 = serverTs - t1;
        TimeSpan csDelta2 = t2 - serverTs;

        while (clockDeltas.Count >= ClockDeltaQueueMaxSize) // remove until one slot free for new clock delta
            clockDeltas.Dequeue();

        clockDeltas.Enqueue(new ClockDelta(csDelta1, csDelta2, Latency: latency));

        await Task.Run(ComputeDelta);

        latestContext = new LatestPlayingContext(DateTimeOffset.UtcNow, playContext);
    }

    private void ComputeDelta()
    {
        int takeCount = Math.Max(clockDeltas.Count / 4, 1);
        IEnumerable<ClockDelta> refDeltas = clockDeltas.OrderBy(d => d.Latency).Take(takeCount);
        TimeSpan[] clockOffsets = refDeltas.SelectMany(d => d.ComputeClockDeltas()).ToArray();

        TimeSpan clockSum = TimeSpan.Zero;
        foreach (TimeSpan co in clockOffsets)
            clockSum += co;

        clockDelta = clockSum / clockOffsets.Length;
    }


    protected override async Task Init()
    {
        await Fetch();
    }

    protected override SpotifyPlayerContext? GetContext()
    {
        Debug.Assert(latestContext is not null);
        (DateTimeOffset contextFetchedOn, CurrentlyPlayingContext? context) = latestContext;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (Math.Abs((contextFetchedOn - now).TotalSeconds) > pollRate)
        {
            Task.Run(Fetch);
        }

        if (context is null || context.CurrentlyPlayingType != "track")
            return null;

        Debug.Assert(this.clockDelta is not null);
        TimeSpan clockDelta = this.clockDelta.Value;

        TimeSpan progress = TimeSpan.FromMilliseconds(context.ProgressMs);
        // Do not allow progress to jump backwards due to clock sync changes
        // We use max tolerance of 1 second so that if the user adjust the music position manually, we don't bug out.
        if (previousProgress is TimeSpan pp && progress < pp && Math.Abs((progress - pp).TotalSeconds) < 1)
            progress = pp;

        FullTrack track = (FullTrack)context.Item;
        return new SpotifyPlayerContext(
            progress,
            context.IsPlaying,
            track.Name,
            TimeSpan.FromMilliseconds(track.DurationMs),
            track.Album.Images[0].Url,
            track.Artists.Select(a => a.Name).ToArray()
        );
    }
}
