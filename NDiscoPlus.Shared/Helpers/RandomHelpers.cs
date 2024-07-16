using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Helpers;
public static class RandomHelpers
{
    /// <summary>
    /// Picks a random element from the list.
    /// </summary>
    public static T Choice<T>(this Random random, IList<T> values)
    {
        return values[random.Next(values.Count)];
    }

    /// <summary>
    /// Pick an item from a list using weights.
    /// </summary>
    /// <param name="random">null for <see cref="Random.Shared"/></param>
    public static T WeightedChoice<T>(this Random random, IList<T> values, IList<int> weights)
    {
        int sum = weights.Sum();
        int rand = random.Next(sum);

        int cumsum = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            int w = weights[i];
            if (w < 0)
                throw new ArgumentException("All weights must be positive.", nameof(weights));

            cumsum += w;
            if (cumsum > rand)
                return values[i];
        }

        throw new Exception("Unreachable.");
    }
}
