using NDiscoPlus.Shared.Effects.Effect;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Music;
internal class MusicEffectGenerator
{
    public IDictionary<EffectIntensity, NDPEffect?> Effects { get; }

    public MusicEffectGenerator(IEnumerable<KeyValuePair<EffectIntensity, NDPEffect?>> effects)
    {
        Effects = effects.ToImmutableDictionary();
    }

    /// <summary>
    /// Create a new music effect generator with a random configuration.
    /// </summary>
    public static MusicEffectGenerator CreateRandom(Random random)
    {
        EffectIntensity[] effectIntensities = Enum.GetValues<EffectIntensity>();

        Dictionary<EffectIntensity, NDPEffect?> effects = effectIntensities
            .Select<EffectIntensity, KeyValuePair<EffectIntensity, NDPEffect?>>(i =>
            {
                IList<NDPEffect> effects = NDPEffect.ByIntensity[i];
                if (effects.Count > 0)
                    return new(i, effects[random.Next(effects.Count)]);
                else
                    return new(i, null);
            })
            .ToDictionary();

        return new MusicEffectGenerator(effects);
    }

    public IEnumerable<NDPEffect> Generate(TrackAudioAnalysis analysis)
    {
        foreach (var section in analysis.Sections)
        {
            EffectIntensity intensity = ComputeIntensity(section);

            NDPEffect? effect = null;
            if (Effects.TryGetValue(intensity, out NDPEffect? v1))
                effect ??= v1;
            if (Effects.TryGetValue(intensity + 1, out NDPEffect? v2))
                effect ??= v2;
            if (Effects.TryGetValue(intensity - 1, out NDPEffect? v3))
                effect ??= v3;

            if (effect is null)
                throw new InvalidOperationException($"No suitable effects found for intensity: {intensity}");

            yield return effect;
        }
    }

    public EffectIntensity ComputeIntensity(Section section)
    {
        double loudnessFactor = ((double)section.Loudness).Remap01(-60, 0);

        // range: 0 - 1
        double totalFactor = loudnessFactor;

        // range: 0 - 5 where 5 is very very rare
        int intensityRef = (int)(totalFactor * 5);

        // range: 1 - 5
        byte intensity = (byte)Math.Clamp(intensityRef + 1, 1, 5);

        return (EffectIntensity)intensity;
    }
}
