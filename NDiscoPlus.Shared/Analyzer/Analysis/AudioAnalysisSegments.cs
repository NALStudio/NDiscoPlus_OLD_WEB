using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Analyzer.Analysis;

internal readonly struct Timbre
{
    public ImmutableArray<float> Values { get; }
    public Timbre(ImmutableArray<float> values)
    {
        Values = values;
    }

    public static double EuclideanDistance(Timbre a, Timbre b)
    {

    }
}

internal readonly struct NDPSegment
{
    public NDPSegment(NDPInterval interval, float confidence, float loudnessStart, float loudnessMax, TimeSpan loudnessMaxTime, float loudnessEnd, ImmutableArray<float> pitches, ImmutableArray<float> timbre)
    {
        Interval = interval;
        Confidence = confidence;
        LoudnessStart = loudnessStart;
        LoudnessMax = loudnessMax;
        LoudnessMaxTime = loudnessMaxTime;
        LoudnessEnd = loudnessEnd;
        Pitches = pitches;
        Timbre = timbre;
    }

    public NDPInterval Interval { get; }

    /// <summary>
    /// <para>The confidence, from 0.0 to 1.0, of the reliability of the segmentation.</para>
    /// <para>Segments of the song which are difficult to logically segment (e.g: noise) may correspond to low values in this field.</para>
    /// </summary>
    public float Confidence { get; }

    /// <summary>
    /// <para>The onset loudness of the segment in decibels (dB).</para>
    /// <para>Combined with loudness_max and loudness_max_time, these components can be used to describe the "attack" of the segment.</para>
    /// </summary>
    public float LoudnessStart { get; }

    /// <summary>
    /// <para>The peak loudness of the segment in decibels (dB).</para>
    /// <para>Combined with loudness_start and loudness_max_time, these components can be used to describe the "attack" of the segment.</para>
    /// </summary>
    public float LoudnessMax { get; }

    /// <summary>
    /// <para>The segment-relative offset of the segment peak loudness.</para>
    /// <para>Combined with loudness_start and loudness_max, these components can be used to desctibe the "attack" of the segment.</para>
    /// </summary>
    public TimeSpan LoudnessMaxTime { get; }

    /// <summary>
    /// <para>The offset loudness of the segment in decibels (dB).</para>
    /// <para>This value should be equivalent to the loudness_start of the following segment.</para>
    /// </summary>
    public float LoudnessEnd { get; }

    public ImmutableArray<float> Pitches { get; }

    public ImmutableArray<float> Timbre { get; }

    public static NDPSegment FromSpotify(SpotifyAPI.Web.Segment segment)
    {
        return new NDPSegment(
            interval: NDPInterval.FromSeconds(segment.Start, segment.Duration),
            confidence: segment.Confidence,
            loudnessStart: segment.LoudnessStart,
            loudnessMax: segment.LoudnessMax,
            loudnessMaxTime: TimeSpan.FromSeconds(segment.LoudnessMaxTime),
            loudnessEnd: segment.LoudnessEnd,
            pitches: segment.Pitches.ToImmutableArray(),
            timbre: segment.Timbre.ToImmutableArray()
        );
    }
}

internal class AudioAnalysisSegments
{
    public AudioAnalysisSegments(ImmutableArray<NDPSegment> segments, ImmutableArray<ImmutableArray<NDPInterval>> bursts)
    {
        Segments = segments;
        Bursts = bursts;
    }

    public ImmutableArray<NDPSegment> Segments { get; }

    public ImmutableArray<ImmutableArray<NDPInterval>> Bursts { get; }
}
