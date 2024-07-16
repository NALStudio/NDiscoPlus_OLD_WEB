using NDiscoPlus.Shared.Effects.BaseEffects;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;

internal class BackgroundContext
{
    public IList<NDPInterval> Bars => bars;
    public IList<NDPInterval> Beats => beats;
    public IList<NDPInterval> Tatums => tatums;

    private readonly ImmutableArray<NDPInterval> bars;
    private readonly ImmutableArray<NDPInterval> beats;
    private readonly ImmutableArray<NDPInterval> tatums;

    public BackgroundContext(TrackAudioAnalysis analysis)
    {
        bars = analysis.Bars.Select(x => (NDPInterval)x).ToImmutableArray();
        beats = analysis.Beats.Select(x => (NDPInterval)x).ToImmutableArray();
        tatums = analysis.Tatums.Select(x => (NDPInterval)x).ToImmutableArray();
    }
}

internal class EffectContextX
{
    public TimeSpan Start { get; }
    public TimeSpan Duration { get; }
    public TimeSpan End => Start + Duration;

    public IList<NDPInterval> Bars => bars;
    public IList<NDPInterval> Beats => beats;
    public IList<NDPInterval> Tatums => tatums;

    private readonly ImmutableArray<NDPInterval> bars;
    private readonly ImmutableArray<NDPInterval> beats;
    private readonly ImmutableArray<NDPInterval> tatums;

    public EffectContextX(Segment segment, IEnumerable<TimeInterval> bars, IEnumerable<TimeInterval> beats, IEnumerable<TimeInterval> tatums)
    {
        Start = TimeSpan.FromSeconds(segment.Start);
        Duration = TimeSpan.FromSeconds(segment.Duration);

        this.bars = bars.Select(x => (NDPInterval)x).ToImmutableArray();
        this.beats = beats.Select(x => (NDPInterval)x).ToImmutableArray();
        this.tatums = tatums.Select(x => (NDPInterval)x).ToImmutableArray();
    }
}
