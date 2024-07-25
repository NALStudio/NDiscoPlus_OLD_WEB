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

    /// <summary>
    /// The overall loudness of the effect's section in decibels (dB).
    /// </summary>
    public double Loudness { get; }

    /// <summary>
    /// <para>The overall estimated tempo of the effect's section in beats per minute (BPM).</para>
    /// <para>In musical terminology, tempo is the speed or pace of a given piece and derives directly from the average beat duration.</para>
    /// </summary>
    public double Tempo { get; }

    /// <summary>
    /// <para>The estimated overall key of the effect's section.</para>
    /// <para>The values in this field ranging from 0 to 11 mapping to pitches using standard Pitch Class notation (E.g. 0 = C, 1 = C♯/D♭, 2 = D, and so on). If no key was detected, the value is -1.</para>
    /// </summary>
    public int Key { get; }

    /// <summary>
    /// <para>Indicates the modality (major or minor) of the effect's section, the type of scale from which its melodic content is derived.</para>
    /// <para>This field will contain a 0 for "minor", a 1 for "major", or a -1 for no result.</para>
    /// <para>Note that the major key (e.g. C major) could more likely be confused with the minor key at 3 semitones lower (e.g. A minor) as both keys carry the same pitches.</para>
    /// </summary>
    public int Mode { get; }

    /// <summary>
    /// <para>An estimated time signature.</para>
    /// <para>The time signature (meter) is a notational convention to specify how many beats are in each bar (or measure).</para>
    /// <para>The value is in the range of [3, 7] indicating time signatures from "3/4", to "7/4".</para>
    /// </summary>
    public int TimeSignature { get; }

    /// <summary>
    /// <para>How many minutes there are in a beat.</para>
    /// <para>Inverse of <see cref="Tempo"/>.</para>
    /// </summary>
    public double MinutesPerBeat => 1d / Tempo;

    /// <summary>
    /// How many seconds there are in a beat.
    /// </summary>
    public double SecondsPerBeat => MinutesPerBeat * 60d;

    /// <summary>
    /// How many seconds there are in a bar.
    /// </summary>
    public double SecondsPerBar => SecondsPerBeat * TimeSignature;

    /// <summary>
    /// <para>The time intervals of the bars throughout the effect's section.</para>
    /// <para>A bar (or measure) is a segment of time defined as <see cref="TimeSignature"/> number of beats.</para>
    /// </summary>
    public IList<NDPInterval> Bars => bars;

    /// <summary>
    /// <para>The time intervals of beats throughout the effect's section.</para>
    /// <para>A beat is the basic time unit of a piece of music; for example, each tick of a metronome.</para>
    /// <para>Beats are typically multiples of tatums.</para>
    /// </summary>
    public IList<NDPInterval> Beats => beats;

    /// <summary>
    /// A tatum represents the lowest regular pulse train that a listener intuitively infers from the timing of perceived musical events (segments).
    /// </summary>
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

internal sealed class BackgroundContext : BaseContext
{
    public BackgroundContext(Random random, NDPColorPalette palette, TrackAudioAnalysis analysis)
        : base(
            random: random,
            palette: palette,
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

internal sealed class EffectContext : BaseContext
{
    private EffectContext(Random random, NDPColorPalette palette, TimeSpan start, TimeSpan duration, double loudness, double tempo, int key, int mode, int timeSignature, IEnumerable<TimeInterval> bars, IEnumerable<TimeInterval> beats, IEnumerable<TimeInterval> tatums)
        : base(
            random: random,
            palette: palette,
            start: start,
            duration: duration,
            loudness: loudness,
            tempo: tempo,
            key: key,
            mode: mode,
            timeSignature: timeSignature,
            bars: bars,
            beats: beats,
            tatums: tatums
        )
    { }

    public static EffectContext Create(Random random, NDPColorPalette palette, TrackAudioAnalysis analysis, Section section)
    {
        static bool StartInsideSectionStart(Section section, TimeInterval interval)
            => TimeSpan.FromSeconds(interval.Start) >= TimeSpan.FromSeconds(section.Start);
        static bool StartInsideSectionEnd(Section section, TimeInterval interval)
        {
            TimeSpan sectionEnd = TimeSpan.FromSeconds(section.Start + section.Duration);
            return TimeSpan.FromSeconds(interval.Start) < sectionEnd;
        }


        return new EffectContext(
            random: random,
            palette: palette,
            start: TimeSpan.FromSeconds(section.Start),
            duration: TimeSpan.FromSeconds(section.Duration),
            loudness: section.Loudness,
            tempo: section.Tempo,
            key: section.Key,
            mode: section.Mode,
            timeSignature: section.TimeSignature,
            bars: analysis.Bars.SkipWhile(b => !StartInsideSectionStart(section, b)).TakeWhile(b => StartInsideSectionEnd(section, b)),
            beats: analysis.Beats.SkipWhile(b => !StartInsideSectionStart(section, b)).TakeWhile(b => StartInsideSectionEnd(section, b)),
            tatums: analysis.Tatums.SkipWhile(t => !StartInsideSectionStart(section, t)).TakeWhile(t => StartInsideSectionEnd(section, t))
        );
    }
}
