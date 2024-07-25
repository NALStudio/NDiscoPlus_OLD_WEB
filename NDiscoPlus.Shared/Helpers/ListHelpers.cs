using System.Diagnostics;

namespace NDiscoPlus.Shared.Helpers;

readonly record struct GroupDistance(int Index, double Distance);

public static class ListHelpers
{
    /// <summary>
    /// start is inclusive, end is exclusive.
    /// </summary>
    private static IEnumerable<T> TakeRange<T>(IList<T> values, int start, int end)
    {
        for (int i = start; i < end; i++)
            yield return values[i];
    }

    /// <summary>
    /// Group close-by objects into n groups. The returned groups are ordered from smallest index to the largest.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1227:Validate arguments correctly")]
    public static IEnumerable<T[]> GroupCloseBy<T>(this IList<T> values, int count, Func<T, T, double> distance)
    {
        if (values.Count < count)
            throw new ArgumentException($"Cannot group {values.Count} values into {count} groups.");

        List<GroupDistance> dists = new(capacity: values.Count - 1);
        for (int i = 1; i < values.Count; i++)
        {
            double d = distance(values[i - 1], values[i]);
            if (d < 0)
                throw new ArgumentException("distance cannot be negative.");
            dists.Add(new GroupDistance(i, d));
        }
        Debug.Assert(dists.Count == values.Count - 1);

        int splitCount = count - 1;
        Debug.Assert(dists.Count >= splitCount);

        int lastIndex = 0;
        foreach (var d in dists.OrderByDescending(d => d.Distance).Take(splitCount).OrderBy(d => d.Index))
        {
            yield return TakeRange(values, lastIndex, d.Index).ToArray();
            lastIndex = d.Index;
        }

        yield return TakeRange(values, lastIndex, values.Count).ToArray();
    }
}
