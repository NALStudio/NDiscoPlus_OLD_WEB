using SpotifyAPI.Web;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Models;

internal class BaseContext
{
    public Random Random { get; }
    public NDPColorPalette Palette { get; }

    public TimeSpan Start { get; }
    public TimeSpan Duration { get; }
    public TimeSpan End => Start + Duration;

    public double Loudness { get; }
    public double Tempo { get; }
    public int Key { get; }
    public int Mode { get; }
    public int TimeSignature { get; }

    public double MinutesPerBeat => 1d / Tempo;
    public double SecondsPerBeat => MinutesPerBeat * 60d;
    public double SecondsPerBar => SecondsPerBeat * TimeSignature;

    public IList<NDPInterval> Bars => bars;
    public IList<NDPInterval> Beats => beats;
    public IList<NDPInterval> Tatums => tatums;

    private readonly ImmutableArray<NDPInterval> bars;
    private readonly ImmutableArray<NDPInterval> beats;
    private readonly ImmutableArray<NDPInterval> tatums;

    public BaseContext(Random random, NDPColorPalette palette, TimeSpan start, TimeSpan duration, double loudness, double tempo, int key, int mode, int timeSignature, IEnumerable<TimeInterval> bars, IEnumerable<TimeInterval> beats, IEnumerable<TimeInterval> tatums)
    {
        Random = random;
        Palette = palette;
        Start = start;
        Duration = duration;
        Loudness = loudness;
        Tempo = tempo;
        Key = key;
        Mode = mode;
        TimeSignature = timeSignature;
        this.bars = bars.Select(x => (NDPInterval)x).ToImmutableArray();
        this.beats = beats.Select(x => (NDPInterval)x).ToImmutableArray();
        this.tatums = tatums.Select(x => (NDPInterval)x).ToImmutableArray();
    }
}

internal class BackgroundContext : BaseContext
{
    public Random Random { get; }

    public TimeSpan Duration { get; }

    public IList<NDPInterval> Bars => bars;
    public IList<NDPInterval> Beats => beats;
    public IList<NDPInterval> Tatums => tatums;

    private readonly ImmutableArray<NDPInterval> bars;
    private readonly ImmutableArray<NDPInterval> beats;
    private readonly ImmutableArray<NDPInterval> tatums;

    public BackgroundContext(Random random, TrackAudioAnalysis analysis) : base(
        random: random,
        start: TimeSpan.Zero,
        duration: TimeSpan.FromSeconds(analysis.Track.Duration),
        loudness: analysis.Track.Loudness,
        tempo: analysis.Track.Tempo,
        key: analysis.Track.Key,
        mode: analysis.Track.Mode,
        timeSignature: analysis.Track.TimeSignature,
        bars: analysis.Bars,
        beats: analysis.Beats,
        tatums: analysis.Tatums
    )
    { }
}

internal class EffectContextX : BaseContext
{
    private EffectContextX(Random random, TimeSpan start, TimeSpan duration, double loudness, double tempo, int key, int mode, int timeSignature, IEnumerable<TimeInterval> bars, IEnumerable<TimeInterval> beats, IEnumerable<TimeInterval> tatums) : base(random, start, duration, loudness, tempo, key, mode, timeSignature, bars, beats, tatums)
    {
    }

    public static EffectContextX Create(Random random, TrackAudioAnalysis analysis, Section section)
    {
        static bool StartIsValid(Section section, TimeInterval interval)
            => TimeSpan.FromSeconds(interval.Start) >= TimeSpan.FromSeconds(section.Start);
        static bool EndIsValid(Section section, TimeInterval interval)
        {
            TimeSpan sectionEnd = TimeSpan.FromSeconds(section.Start) + TimeSpan.FromSeconds(section.Duration);
            TimeSpan intervalEnd = TimeSpan.FromSeconds(interval.Start) + TimeSpan.FromSeconds(interval.Duration);
            return intervalEnd < sectionEnd;
        }

        TimeSpan start = TimeSpan.FromSeconds(section.Start);
        TimeSpan duration = TimeSpan.FromSeconds(section.Duration);

        return new EffectContextX(
            random: random,
            start: start,
            duration: duration,
            loudness: section.Loudness,
            tempo: section.Tempo,
            key: section.Key,
            mode: section.Mode,
            timeSignature: section.TimeSignature,
            bars: analysis.Bars.SkipWhile(b => !StartIsValid(section, b)).TakeWhile(b => EndIsValid(section, b)),
            beats: analysis.Beats.SkipWhile(b => !StartIsValid(section, b)).TakeWhile(b => EndIsValid(section, b)),
            tatums: analysis.Tatums.SkipWhile(t => !StartIsValid(section, t)).TakeWhile(t => EndIsValid(section, t))
        );
    }
}
