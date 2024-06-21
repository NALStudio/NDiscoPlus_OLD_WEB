using NDiscoPlus.Shared.Effects.Effects;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System.Collections.Immutable;
using System.Diagnostics;

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
    private record struct IntensityComparison(double Tempo, double Loudness)
    {
        public static implicit operator IntensityComparison(Section section)
            => new(Tempo: section.Tempo, Loudness: section.Loudness);

        public static implicit operator IntensityComparison(TrackAudioFeatures features)
            => new(Tempo: features.Tempo, Loudness: features.Loudness);
    }

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
        EffectIntensity? intensity = null;

        for (int i = 0; i < args.Analysis.Sections.Count; i++)
        {
            Section currentSection = args.Analysis.Sections[i];

            // As some sections that come after drops are actually quieter,
            // it might be beneficial to compare the strobes for x seconds
            // before and after the section change to determine if a drop happened
            intensity = ComputeContextAwareIntensity(
                intensity,
                previousSection: i > 0 ? args.Analysis.Sections[i - 1] : null,
                args.Features,
                section: currentSection,
                isLastSection: i >= (args.Analysis.Sections.Count - 1)
            );

            NDPEffect? effect = null;
            if (Effects.TryGetValue(intensity.Value, out NDPEffect? v1))
                effect ??= v1;
            if (Effects.TryGetValue(intensity.Value + 1, out NDPEffect? v2))
                effect ??= v2;
            if (Effects.TryGetValue(intensity.Value - 1, out NDPEffect? v3))
                effect ??= v3;

            yield return EffectRecord.FindAndCreate(effect, currentSection);
        }
    }

    private static EffectIntensity ComputeContextAwareIntensity(
        EffectIntensity? previousIntensity,
        Section? previousSection,
        TrackAudioFeatures features,
        Section section,
        bool isLastSection
    )
    {
        if (previousIntensity is EffectIntensity pIntensity)
        {
            Debug.Assert(previousSection is not null);

            int intensity = (int)pIntensity;

            double intensityComparison = CompareIntensities(section, previousSection);
            if (intensityComparison >= 0)
            {
                intensity++;
            }
            else
            {
                if (isLastSection && intensityComparison > -0.1d)
                    intensity++;
                else
                    intensity--;
            }

            if (intensity > (int)maxIntensity)
                intensity = (int)maxIntensity;
            else if (intensity < (int)minIntensity)
                intensity = (int)minIntensity;

            return (EffectIntensity)intensity;
        }
        else
        {
            Debug.Assert(previousSection is null);

            int intensity = (int)IntensityFromFeatures(features);

            // if section is less intensive than features, decrement intensity if possible
            // we do this to be extra conservative with the first section's intensity
            // as it determines the rest of the song's intensity and usually is one of the calmer parts of the song.
            if (intensity > (int)minIntensity && CompareIntensities(section, features) < 0)
                intensity--;

            return (EffectIntensity)intensity;
        }
    }

    /// <summary>
    /// Compute an intensity value (approximately in the range of -1 to 1) for a in relation to b.
    /// </summary>
    private static double CompareIntensities(IntensityComparison a, IntensityComparison b)
    {
        // when a.Tempo == b.Tempo => (1d) - 1d => 0d
        // when a.Tempo > b.Tempo => for example (2d) - 1d => 1d;
        double[] refCollection = [
            (a.Tempo / b.Tempo) - 1d,
            (a.Loudness / b.Loudness) - 1d
        ];

        return refCollection.Sum() / refCollection.Length;
    }

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
