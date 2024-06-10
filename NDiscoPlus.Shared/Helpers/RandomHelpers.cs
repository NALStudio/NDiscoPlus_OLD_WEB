using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Helpers;
internal static class RandomHelpers
{
    /// <summary>
    /// Pick an item from a list using weights.
    /// </summary>
    /// <param name="random">null for <see cref="Random.Shared"/></param>
    public static T WeightedRandom<T>(IList<T> values, IList<int> weights, Random? random)
    {
        int sum = weights.Sum();
        int rand = (random ?? Random.Shared).Next(sum);

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
