using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NDiscoPlus.Shared.Helpers;


public static class ListHelpers
{
    private readonly record struct GroupDistance<TDist>(int Index, TDist DistanceFromPrevious) where TDist : INumberBase<TDist>;

    /// <summary>
    /// start is inclusive, end is exclusive.
    /// </summary>
    private static IEnumerable<T> TakeRange<T>(IReadOnlyList<T> values, int start, int end)
    {
        for (int i = start; i < end; i++)
            yield return values[i];
    }

    /// <summary>
    /// <para>Split the given values list at each given index.</para>
    /// <para>The index determines the start of the next section.</para>
    /// </summary>
    private static IEnumerable<T[]> SplitAtIndexes<T>(IReadOnlyList<T> values, IEnumerable<int> indexes)
    {
        int lastIndex = 0;
        foreach (int index in indexes)
        {
            yield return TakeRange(values, lastIndex, index).ToArray();
            lastIndex = index;
        }

        yield return TakeRange(values, lastIndex, values.Count).ToArray();
    }

    private static List<GroupDistance<TDist>> ComputeDistances<T, TDist>(IReadOnlyList<T> values, Func<T, T, TDist> distance) where TDist : INumberBase<TDist>, IComparable<TDist>
    {
        List<GroupDistance<TDist>> dists = new(capacity: values.Count - 1);
        for (int i = 1; i < values.Count; i++)
        {
            int prev = i - 1;
            TDist d = distance(values[prev], values[i]);
            if (d.CompareTo(TDist.Zero) < 0)
                throw new ArgumentException("distance cannot be negative.");
            dists.Add(new(i, d));
        }

        Debug.Assert(dists.Count == values.Count - 1);
        return dists;
    }

    private static IEnumerable<T[]> SplitDistances<T, TDist>(IReadOnlyList<T> values, int splitCount, in List<GroupDistance<TDist>> distances) where TDist : INumberBase<TDist>, IComparable<TDist>
    {
        if (distances.Count < splitCount)
            throw new ArgumentException("distances count must be more than the split count", nameof(distances));

        // compare b to a so that distances are sorted from largest to smallest
        distances.Sort((a, b) => b.DistanceFromPrevious.CompareTo(a.DistanceFromPrevious));
        CollectionsMarshal.SetCount(distances, splitCount);
        distances.Sort((a, b) => a.Index.CompareTo(b.Index));

        return SplitAtIndexes(values, distances.Select(d => d.Index));
    }

    /// <summary>
    /// <para>Split values into <paramref name="count"/> groups by the longest distance.</para>
    /// </summary>
    public static IEnumerable<T[]> GroupCloseBy<T, TDist>(this IReadOnlyList<T> values, int count, Func<T, T, TDist> distance) where TDist : INumberBase<TDist>, IComparable<TDist>
    {
        static IEnumerable<T[]> DebugAssertCountMatches(IEnumerable<T[]> values, int count)
        {
            int c = 0;
            foreach (T[] v in values)
            {
                yield return v;
                c++;
            }

            Debug.Assert(c == count);
        }

        if (values.Count < count)
            throw new ArgumentException($"Cannot group {values.Count} values into {count} groups.");

        List<GroupDistance<TDist>> distances = ComputeDistances(values, distance);
        int splitCount = count - 1;
        IEnumerable<T[]> output = SplitDistances(values, splitCount, in distances);

#if DEBUG
        return DebugAssertCountMatches(output, count);
#else
        return output;
#endif
    }

    /// <summary>
    /// <para>Split values into <paramref name="count"/> groups by the longest distance.</para>
    /// <para>
    /// Use <paramref name="tolerance"/> to adjust which values are considered to be the "same".
    /// The values that are within tolerance are split evenly across groups.
    /// </para>
    /// </summary>
    public static IEnumerable<T[]> GroupCloseBy<T, TDist>(this IReadOnlyList<T> values, int count, Func<T, T, TDist> distance, TDist tolerance) where TDist : INumberBase<TDist>, IComparable<TDist>
    {
        // Compare rightmost in a with leftmost in b.
        static TDist GroupDistanceFunc(T[] a, T[] b, Func<T, T, TDist> distance) => distance(a[^1], b[0]);

        if (values.Count < count)
            throw new ArgumentException($"Cannot group {values.Count} values into {count} groups.");

        List<GroupDistance<TDist>> distances = ComputeDistances(values, distance);

        // group values
        List<T[]> valuesGrouped = new(capacity: values.Count);
        int groupStart = 0;
        for (int i = 0; i < distances.Count; i++)
        {
            GroupDistance<TDist> d = distances[i];
            if (d.DistanceFromPrevious.CompareTo(tolerance) <= 0)
                continue;

            int groupEnd = d.Index; // exclusive
            valuesGrouped.Add(TakeRange(values, groupStart, groupEnd).ToArray());
            groupStart = groupEnd;
        }
        valuesGrouped.Add(TakeRange(values, groupStart, values.Count).ToArray());

        // if grouped values count is larger than the target count, group them by the shortest distance between the group's items
        if (valuesGrouped.Count >= count)
            return GroupCloseBy(valuesGrouped, count, (a, b) => GroupDistanceFunc(a, b, distance)).Select(x => x.SelectMany(y => y).ToArray());

        // otherwise split the longest groups
        int groupSplitCount = count - valuesGrouped.Count;
        foreach (T[] groupToSplit in valuesGrouped.OrderByDescending(g => g.Length).Take(groupSplitCount))
        {
            int groupIndex = valuesGrouped.IndexOf(groupToSplit);
            Debug.Assert(groupIndex != -1);

            T[] group = valuesGrouped[groupIndex];

            int splitIndex = group.Length / 2;
            T[] left = group[..splitIndex];
            T[] right = group[splitIndex..];

            valuesGrouped[groupIndex] = right;
            valuesGrouped.Insert(groupIndex, left); // left insert pushes right... well... right.
        }

        Debug.Assert(valuesGrouped.Count == count);
        return valuesGrouped;
    }
}
