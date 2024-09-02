namespace NDiscoPlus.Shared.Helpers;


public static class ListHelpers
{
    private readonly record struct ValuePosition<T>(T Value, double Position)
    {
        public void Deconstruct(out T value, out double position)
        {
            value = Value;
            position = Position;
        }
    }

    private readonly record struct PositionGroup<T>(double GroupPosition, List<T> Values);

    /// <summary>
    /// Chunk <paramref name="values"/> values into <paramref name="groups"/> groups by position.
    /// </summary>
    public static IEnumerable<T[]> ChunkByPositionByGroupNumber<T>(this IReadOnlyList<T> values, int groups, Func<T, double> positionSelector)
    {
        static double Distance(double a, double b)
            => Math.Abs(a - b);

        ArgumentOutOfRangeException.ThrowIfLessThan(groups, 1, nameof(groups));

        if (values.Count < 1)
            return Enumerable.Repeat(Array.Empty<T>(), groups).ToList();

        // Sort by position
        ValuePosition<T>[] positions = values.Select(v => new ValuePosition<T>(v, positionSelector(v))).ToArray();
        Array.Sort(positions, (a, b) => a.Position.CompareTo(b.Position));

        // Generate group position points
        // these are kind of like hooks where the closest values will cling to.
        double minPos = positions[0].Position;
        double maxPos = positions[^1].Position;

        // Create groups
        PositionGroup<T>[] grouped = new PositionGroup<T>[groups];
        for (int i = 0; i < groups; i++)
        {
            double groupPos = DoubleHelpers.LerpUnclamped(minPos, maxPos, i / (double)(groups - 1));
            List<T> groupValues = new();
            grouped[i] = new PositionGroup<T>(groupPos, groupValues);
        }

        // Assign values to closest group (by group position point)
        int closestIndex = 0;
        foreach ((T val, double pos) in positions) // values are ordered from minPos to maxPos
        {
            int lastIndex = groups - 1;
            if (closestIndex < lastIndex) // when we get to the last index, the closest group cannot be any further right so we don't check distances anymore
            {
                int nextIndex = closestIndex + 1;

                double leftDist = Distance(grouped[closestIndex].GroupPosition, pos);
                double rightDist = Distance(pos, grouped[nextIndex].GroupPosition);

                if (rightDist < leftDist)
                    closestIndex = nextIndex;
            }

            grouped[closestIndex].Values.Add(val);
        }

        return grouped.Select(g => g.Values.ToArray());
    }
}
