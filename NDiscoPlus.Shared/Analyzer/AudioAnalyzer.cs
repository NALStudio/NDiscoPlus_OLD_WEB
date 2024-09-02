using NDiscoPlus.Shared.Analyzer.Analysis;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Analyzer;
internal static class AudioAnalyzer
{
    public static AudioAnalysis Analyze(TrackAudioFeatures features, TrackAudioAnalysis analysis)
    {
        static ImmutableArray<NDPInterval> CastSpotifyIntervals(List<TimeInterval> spotifyIntervals)
            => spotifyIntervals.Select(static interval => (NDPInterval)interval).ToImmutableArray();

        AudioAnalysisTimings timings = new(
            bars: CastSpotifyIntervals(analysis.Bars),
            beats: CastSpotifyIntervals(analysis.Beats),
            tatums: CastSpotifyIntervals(analysis.Tatums)
        );

        ImmutableArray<NDPInterval> segmentsArray = analysis.Segments.Select(s => NDPInterval.FromSeconds(s.Start, s.Duration)).ToImmutableArray();
        AudioAnalysisSegments segments = new(
            segments: segmentsArray,
            bursts: AnalyzeBursts(segmentsArray).ToImmutableArray()
        );

        ImmutableArray<AudioAnalysisSection> sections = AnalyzeSections(analysis)
                                                            .Select(sec => AudioAnalysisSection.ConstructFromSpotify(timings, sec.OriginalSection, sec.AdjustedInterval))
                                                            .ToImmutableArray();


        return new AudioAnalysis(
            track: AudioAnalysisTrack.FromSpotify(analysis.Track),
            features: AudioAnalysisFeatures.FromSpotify(features),
            timings: timings,
            sections: sections,
            segments: segments
        );
    }

    private static IEnumerable<(Section OriginalSection, NDPInterval? AdjustedInterval)> AnalyzeSections(TrackAudioAnalysis analysis)
    {
        foreach (Section section in analysis.Sections)
            yield return (section, null);
    }

    // Bursts are a collection of multiple short (and approximately equal length) segments
    private static IEnumerable<ImmutableArray<NDPInterval>> AnalyzeBursts(ImmutableArray<NDPInterval> segments)
    {
        const double MaxDurationDifferenceRatio = 0.15d;

        const int MinBurstLength = 4;

        static bool SegmentCanBeInTheSameBurst(NDPInterval segment1, NDPInterval segment2)
        {
            // if (segment.Duration.TotalSeconds > MaxSegmentDurationSeconds)
            //     return false;

            // ratio valid values: 0,9 - 1,1   (when MaxDurationDifference 0,1)
            double ratio = segment1.Duration.TotalSeconds / segment2.Duration.TotalSeconds;

            // diff valid values: -0,1 - 0,1   (when MaxDurationDifference 0,1)
            double diff = ratio - 1d;

            return Math.Abs(diff) <= MaxDurationDifferenceRatio;
        }

        // TODO: Minimum burst loudness
        static bool BurstIsValid(IReadOnlyList<NDPInterval> burst)
        {
            if (burst.Count < MinBurstLength)
                return false;

            // TODO: Play with these values to make the detection more accurate
            // Currently it is two lenient and has a lot of misfires, consider reducing maxDur
            const double minDur = 0.15;
            const double maxDur = 0.3;
            double maxSegmentDurationSeconds = ((double)burst.Count).Remap(4, 12, minDur, maxDur)
                                                                    .Clamp(minDur, maxDur);

            // no need to verify empty list behaviour with .Any() as burst count is already verified
            if (burst.Any(b => b.Duration.TotalSeconds > maxSegmentDurationSeconds))
                return false;

            return true;
        }

        static List<NDPInterval> MergeBursts(IReadOnlyList<NDPInterval> left, NDPInterval mid, IReadOnlyList<NDPInterval> right)
        {
            NDPInterval l = left[^1];
            NDPInterval r = right[0];

            TimeSpan averageDuration = (l.Duration + r.Duration) / 2;

            // mid is a different length than the average duration of the burst
            // if it's longer, we have to split it
            int midSubsectionCount = (int)Math.Round(mid.Duration.TotalSeconds / averageDuration.TotalSeconds);
            if (midSubsectionCount < 1) // if mid is shorter than the average duration, just display mid as normal
                midSubsectionCount = 1;

            List<NDPInterval> output = new(capacity: left.Count + midSubsectionCount + right.Count);
            output.AddRange(left);

            if (midSubsectionCount > 1)
            {
                TimeSpan subsectionDuration = mid.Duration / midSubsectionCount;
                for (int i = 0; i < midSubsectionCount; i++)
                {
                    TimeSpan newStart = mid.Start + (i * subsectionDuration);
                    if (i < (midSubsectionCount - 1))
                        output.Add(new NDPInterval(newStart, subsectionDuration));
                    else
                        output.Add(new NDPInterval(newStart, mid.Duration - newStart));
                }
            }
            else
            {
                output.Add(mid);
            }

            output.AddRange(right);

            return output;
        }

        List<List<NDPInterval>> bursts = new() { new List<NDPInterval>() };

        foreach (NDPInterval segment in segments)
        {
            // we initialize bursts list with a value so bursts.Count should always be above 0
            List<NDPInterval> lastBurst = bursts[^1];

            // if difference was over limit (segment is not valid to be appended to the burst)
            if (lastBurst.Count > 0 && !SegmentCanBeInTheSameBurst(segment, lastBurst[^1]))
            {
                // start building a new burst
                bursts.Add(new());
            }

            // add segment to last burst
            bursts[^1].Add(segment);
        }

        // Merge neighbouring bursts that are separated by one-element longer burst
        // (Spotify analysis sometimes merges two short segments into a longer segment)

        // Add items into a temporary buffer before removal so that list can be iterated safely.
        List<(int StartIndex, int Count, List<NDPInterval> MergedBurst)> merged = new();

        for (int i = 1; i < (bursts.Count - 1); i++)
        {
            int startIndex = i - 1;
            int endIndex = i + 1;  // inclusive
            int count = (endIndex - startIndex) + 1; // +1 because end is inclusive

            List<NDPInterval> left = bursts[startIndex];
            List<NDPInterval> mid = bursts[i];
            List<NDPInterval> right = bursts[endIndex];

            if (left.Count <= 1)
                continue;
            if (mid.Count != 1)
                continue;
            if (right.Count <= 1)
                continue;
            // left.Count > 1; mid.Count == 1; right.Count > 1

            // Do not check.
            // If there are two burst side by side separated by one longer/shorter segment
            // we want to always merge them as otherwise two bursts one after another looks quite odd (the animations don't line up)
            // if (!SegmentCanBeInTheSameBurst(left[^1], right[0]))
            //     continue;

            merged.Add((startIndex, count, MergeBursts(left, mid[0], right)));
        }

        // replace bursts with their merged variants
        // order by descending so that we can safely remove the value without affecting other indexes that haven't yet been removed
        foreach ((int startIndex, int count, List<NDPInterval> merge) in merged.OrderByDescending(x => x.StartIndex))
        {
            bursts.RemoveRange(startIndex, count);
            bursts.Insert(startIndex, merge);
        }


        // enumerate output
        foreach (List<NDPInterval> burst in bursts)
        {
            if (BurstIsValid(burst))
                yield return burst.ToImmutableArray();
        }
    }
}
