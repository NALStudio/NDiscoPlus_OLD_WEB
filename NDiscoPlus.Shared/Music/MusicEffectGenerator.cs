using NDiscoPlus.Shared.Effects.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Music;

public record EffectRecord
{
    public int? EffectIndex { get; init; }
    internal NDPEffect? Effect => EffectIndex.HasValue ? NDPEffect.All[EffectIndex.Value] : null;

    public Section Section { get; init; }
    public NDPInterval Interval { get; init; }

    public EffectRecord(int? effectIndex, Section section)
    {
        EffectIndex = effectIndex;
        Section = section;
        Interval = NDPInterval.FromSeconds(section.Start, section.Duration);
    }

    internal static EffectRecord FindAndCreate(NDPEffect? effect, Section section)
    {
        int? effectIndex;
        if (effect is null)
            effectIndex = null;
        else
            effectIndex = NDPEffect.All.IndexOf(effect);

        return new EffectRecord(effectIndex, section);
    }
}

internal class MusicEffectGenerator
{
    public IDictionary<EffectIntensity, NDPEffect?> Effects { get; }

    private static readonly EffectIntensity minIntensity = Enum.GetValues<EffectIntensity>().Min();
    private static readonly EffectIntensity maxIntensity = Enum.GetValues<EffectIntensity>().Max();

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

    public IEnumerable<EffectRecord> Generate(NDiscoPlusArgs args)
    {
        foreach (var section in args.Analysis.Sections)
        {
            double MinutesPerBeat = 1d / section.Tempo;
            double SecondsPerBeat = MinutesPerBeat * 60d;
            double SecondsPerBar = SecondsPerBeat * 60d;

            EffectIntensity intensity = ComputeSectionIntensity(args.Features, section);

            NDPEffect? effect = null;
            if (Effects.TryGetValue(intensity, out NDPEffect? v1))
                effect ??= v1;
            if (Effects.TryGetValue(intensity + 1, out NDPEffect? v2))
                effect ??= v2;
            if (Effects.TryGetValue(intensity - 1, out NDPEffect? v3))
                effect ??= v3;

            yield return EffectRecord.FindAndCreate(effect, section);
        }
    }

    private EffectIntensity ComputeSectionIntensity(TrackAudioFeatures features, Section section)
    {
        int baseIntensity = (int)IntensityFromFeatures(features);

        // when section.Tempo == features.Tempo => (1d) - 1d => 0d
        // when section.Tempo > features.Tempo => for example (2d) - 1d => 1d;
        double tempoRef = (section.Tempo / features.Tempo) - 1d;
        double loudnessRef = (section.Loudness / features.Loudness) - 1d;

        double[] refCollection = [
            tempoRef,
            loudnessRef
        ];

        double reference = refCollection.Sum() / refCollection.Length;

        // TODO: Increment / decrement the intensity on section change based on previous intensity
        // starting reference must be conservative
        // this way we can ensure that we get more effects than just the default.
        int intensity;
        if (Math.Abs(reference) < 0.2d)
            intensity = baseIntensity;
        else if (reference > 0)
            intensity = baseIntensity + 1;
        else
            intensity = baseIntensity - 1;

        int clampedIntensity = Math.Clamp(intensity, (int)minIntensity, (int)maxIntensity);
        return (EffectIntensity)clampedIntensity;
    }

    /// <summary>Convert a 0 - 1 value to an <see cref="EffectIntensity"/> instance.</summary>
    private static EffectIntensity IntensityFromFeatures(TrackAudioFeatures features)
    {
        // range: 0 - 4 where 4 is very rare
        int intensityRef = (int)(features.Energy * 4f);

        // range: 1 - 5 where 5 is very rare
        int intensity = intensityRef + 1;

        // EffectIntensity.VeryHigh is very rare
        return (EffectIntensity)intensity;
    }
}
