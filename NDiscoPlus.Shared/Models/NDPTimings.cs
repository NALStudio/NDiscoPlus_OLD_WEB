using SpotifyAPI.Web;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

public readonly record struct NDPInterval(TimeSpan Start, TimeSpan Duration)
{
    public TimeSpan End => Start + Duration;

    public bool Contains(TimeSpan t)
    {
        return t >= Start && t <= End;
    }
}

public class NDPTimings
{
    public NDPTimings(ImmutableList<NDPInterval> bars, ImmutableList<NDPInterval> beats, ImmutableList<NDPInterval> tatums)
    {
        this.bars = bars;
        this.beats = beats;
        this.tatums = tatums;
    }

    [JsonInclude]
    private ImmutableList<NDPInterval> bars;
    [JsonIgnore]
    public IList<NDPInterval> Bars => bars;

    [JsonInclude]
    private ImmutableList<NDPInterval> beats;
    [JsonIgnore]
    public IList<NDPInterval> Beats => beats;

    [JsonInclude]
    private ImmutableList<NDPInterval> tatums;
    [JsonIgnore]
    public IList<NDPInterval> Tatums => tatums;


    public static NDPTimings FromAnalysis(TrackAudioAnalysis analysis)
    {
        return new NDPTimings(
            bars: analysis.Bars.Select(b => new NDPInterval(TimeSpan.FromSeconds(b.Start), TimeSpan.FromSeconds(b.Duration))).ToImmutableList(),
            beats: analysis.Beats.Select(b => new NDPInterval(TimeSpan.FromSeconds(b.Start), TimeSpan.FromSeconds(b.Duration))).ToImmutableList(),
            tatums: analysis.Tatums.Select(b => new NDPInterval(TimeSpan.FromSeconds(b.Start), TimeSpan.FromSeconds(b.Duration))).ToImmutableList()
        );
    }
}
