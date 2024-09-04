using NDiscoPlus.Shared.Analyzer.Analysis;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Analyzer;

// Bursts are a collection of multiple short (and approximately equal length) segments
internal static class AudioAnalyzerBurst
{
    private readonly record struct BurstNode(NDPInterval Interval, TimeSpan SegmentOffset, NDPSegment SegmentReference)
    {
        public BurstNode(TimeSpan Start, TimeSpan Duration, TimeSpan SegmentOffset, NDPSegment SegmentReference) : this(
            Interval: new(Start, Duration),
            SegmentOffset: SegmentOffset,
            SegmentReference: SegmentReference
        )
        { }

        public static BurstNode NoOffset(NDPSegment segment)
        {
            return new(
                Interval: segment.Interval,
                SegmentOffset: TimeSpan.Zero,
                SegmentReference: segment
            );
        }
    }

    const double MaxDurationDifferenceRatio = 0.15d;
    const int MinBurstLength = 4;

    private static bool SegmentCanBeInTheSameBurst(NDPInterval segment1, NDPInterval segment2)
    {
        // if (segment.Duration.TotalSeconds > MaxSegmentDurationSeconds)
        //     return false;

        // ratio valid values: 0,9 - 1,1   (when MaxDurationDifference 0,1)
        double ratio = segment1.Duration.TotalSeconds / segment2.Duration.TotalSeconds;

        // diff valid values: -0,1 - 0,1   (when MaxDurationDifference 0,1)
        double diff = ratio - 1d;

        return Math.Abs(diff) <= MaxDurationDifferenceRatio;
    }

    private static bool BurstIsValid(IReadOnlyList<BurstNode> burst)
    {
        static bool VerifyDuration(BurstNode burst, int burstNodeCount)
        {
            const double minDur = 0.15;
            const double maxDur = 0.20;
            double maxSegmentDurationSeconds = ((double)burstNodeCount).Remap(4, 12, minDur, maxDur)
                                                                       .Clamp(minDur, maxDur);

            return burst.Interval.Duration.TotalSeconds < maxSegmentDurationSeconds;
        }

        static bool VerifyConfidence(BurstNode burst)
        {
            double minConfidence = burst.Interval.Duration.TotalSeconds.Remap((0.1, 0.2), (0.3, 0.6));
            return burst.SegmentReference.Confidence > minConfidence;
        }

        if (burst.Count < MinBurstLength)
            return false;

        // no need to verify empty list behaviour with .Any() as burst count is already verified
        if (!burst.All(b => VerifyDuration(b, burst.Count)))
            return false;

        // at least two thirds of burst segments must have a valid confidence value
        int minConfidentSegments = (int)((2d / 3d) * burst.Count);
        if (burst.Count(static b => VerifyConfidence(b)) < minConfidentSegments)
            return false;

        // minimum burst loudness ???

        return true;
    }

    private static List<BurstNode> MergeBursts(IReadOnlyList<BurstNode> left, BurstNode mid, IReadOnlyList<BurstNode> right)
    {
        BurstNode l = left[^1];
        BurstNode r = right[0];

        TimeSpan averageDuration = (l.Interval.Duration + r.Interval.Duration) / 2;

        // mid is a different length than the average duration of the burst
        // if it's longer, we have to split it
        int midSubsectionCount = (int)Math.Round(mid.Interval.Duration.TotalSeconds / averageDuration.TotalSeconds);
        if (midSubsectionCount < 1) // if mid is shorter than the average duration, just display mid as normal
            midSubsectionCount = 1;

        List<BurstNode> output = new(capacity: left.Count + midSubsectionCount + right.Count);
        output.AddRange(left);

        if (midSubsectionCount > 1)
        {
            TimeSpan subsectionDuration = mid.Interval.Duration / midSubsectionCount;
            for (int i = 0; i < midSubsectionCount; i++)
            {
                TimeSpan startOffset = i * subsectionDuration;
                TimeSpan newStart = mid.Interval.Start + startOffset;
                if (i < (midSubsectionCount - 1))
                    output.Add(new BurstNode(newStart, subsectionDuration, startOffset, mid.SegmentReference));
                else
                    output.Add(new BurstNode(newStart, mid.Interval.Duration - newStart, startOffset, mid.SegmentReference));
            }
        }
        else
        {
            output.Add(mid);
        }

        output.AddRange(right);

        return output;
    }

    // TODO: How to differentiate correct bursts from incorrect ones?
    // Now, some very important bursts are missing and some unnecessary ones exist...
    public static IEnumerable<ImmutableArray<NDPInterval>> AnalyzeBursts(ImmutableArray<NDPSegment> segments)
    {
        List<List<BurstNode>> bursts = new() { new List<BurstNode>() };

        foreach (NDPSegment segment in segments)
        {
            // we initialize bursts list with a value so bursts.Count should always be above 0
            List<BurstNode> lastBurst = bursts[^1];
            // take the last node of the last burst, the length of this list is not guaranteed to be above 0 due to bursts being initialized with an empty sublist
            BurstNode? lastBurstNode = lastBurst.Count > 0 ? lastBurst[^1] : null;

            // skip checking if segment fits into burst if burst does not have any segments to compare against
            if (lastBurstNode is BurstNode burstNode)
            {
                Debug.Assert(burstNode.SegmentOffset == TimeSpan.Zero);
                // if difference was over limit (segment is not valid to be appended to the burst)
                if (!SegmentCanBeInTheSameBurst(segment.Interval, burstNode.Interval))
                {
                    // start building a new burst
                    bursts.Add(new());
                }
            }

            // add segment to last burst
            bursts[^1].Add(BurstNode.NoOffset(segment));
        }

        // Merge neighbouring bursts that are separated by one-element longer burst
        // (Spotify analysis sometimes merges two short segments into a longer segment)

        // Add items into a temporary buffer before removal so that list can be iterated safely.
        List<(int StartIndex, int Count, List<BurstNode> MergedBurst)> merged = new();

        for (int i = 1; i < (bursts.Count - 1); i++)
        {
            int startIndex = i - 1;
            int endIndex = i + 1;  // inclusive
            int count = (endIndex - startIndex) + 1; // +1 because end is inclusive

            List<BurstNode> left = bursts[startIndex];
            List<BurstNode> midNodes = bursts[i];
            List<BurstNode> right = bursts[endIndex];

            if (midNodes.Count != 1)
                continue;
            if (left.Count < 1)
                continue;
            if (right.Count < 1)
                continue;

            BurstNode mid = midNodes[0];

            // If there is only one element in left or right, only allow merge after some additional checks
            if (left.Count == 1 || right.Count == 1)
            {
                NDPInterval prev = left[^1].Interval;
                NDPInterval next = right[0].Interval;

                bool prevNextCanBeMerged = SegmentCanBeInTheSameBurst(prev, next);

                Debug.Assert(mid.SegmentOffset == TimeSpan.Zero);
                NDPSegment midSegment = mid.SegmentReference;

                // some short segments are conjoined together in the Spotify analysis (i.e. they form a segment with double the length)
                // usually in these cases the length is around double the left or right length and loudness max time is in the middle
                NDPInterval midSplit1 = new(midSegment.Interval.Start, midSegment.LoudnessMaxTime);
                NDPInterval midSplit2 = new(midSegment.Interval.Start + midSegment.LoudnessMaxTime, midSegment.Interval.Duration - midSegment.LoudnessMaxTime);

                bool prevMidCanBeMerged = SegmentCanBeInTheSameBurst(prev, midSplit1);
                bool nextMidCanBeMerged = SegmentCanBeInTheSameBurst(midSplit2, next);

                if (!(prevNextCanBeMerged && prevMidCanBeMerged && nextMidCanBeMerged))
                    continue;
            }

            // Do not check.
            // If there are two burst side by side separated by one longer/shorter segment
            // we want to always merge them as otherwise two bursts one after another looks quite odd (the animations don't line up)
            // if (!SegmentCanBeInTheSameBurst(left[^1], right[0]))
            //     continue;

            merged.Add((startIndex, count, MergeBursts(left, mid, right)));
        }

        // replace bursts with their merged variants
        // order by descending so that we can safely remove the value without affecting other indexes that haven't yet been removed
        foreach ((int startIndex, int count, List<BurstNode> merge) in merged.OrderByDescending(x => x.StartIndex))
        {
            bursts.RemoveRange(startIndex, count);
            bursts.Insert(startIndex, merge);
        }


        // enumerate output
        foreach (List<BurstNode> burst in bursts)
        {
            if (BurstIsValid(burst))
                yield return burst.Select(b => b.Interval).ToImmutableArray();
        }
    }
}
