using NDiscoPlus.Shared.Effects.Effects;
using NDiscoPlus.Shared.Helpers;
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

public class MusicEffectGenerator
{
    private record struct IntensityComparison(double Tempo, double Loudness)
    {
        public static implicit operator IntensityComparison(Section section)
            => new(Tempo: section.Tempo, Loudness: section.Loudness);

        public static implicit operator IntensityComparison(TrackAudioFeatures features)
            => new(Tempo: features.Tempo, Loudness: features.Loudness);
    }

    internal IDictionary<EffectIntensity, NDPEffect?> Effects { get; }

    private static readonly EffectIntensity minIntensity = Enum.GetValues<EffectIntensity>().Min();
    private static readonly EffectIntensity maxIntensity = Enum.GetValues<EffectIntensity>().Max();

    private static EffectIntensity ClampIntensity(int intensity)
    {
        if (intensity > (int)maxIntensity)
            return maxIntensity;
        if (intensity < (int)minIntensity)
            return minIntensity;
        return (EffectIntensity)intensity;
    }

    internal MusicEffectGenerator(IEnumerable<KeyValuePair<EffectIntensity, NDPEffect?>> effects)
    {
        Effects = effects.ToImmutableDictionary();
    }

    /// <summary>
    /// Create a new music effect generator with a random configuration.
    /// </summary>
    internal static MusicEffectGenerator CreateRandom(Random random)
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

    internal IEnumerable<EffectRecord> Generate(NDiscoPlusArgs args)
    {
        foreach ((EffectIntensity intensity, Section section) in ComputeIntensities(args))
        {
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

    public static IEnumerable<(EffectIntensity intensity, Section section)> ComputeIntensities(NDiscoPlusArgs args)
    {
        TrackAudioFeatures features = args.Features;
        TrackAudioAnalysis analysis = args.Analysis;

        int? previousIntensity = null;
        bool previousWasDoubleJumped = false;

        Section? loudestSection = analysis.Sections.MaxBy(s => s.Loudness);

        // TODO: Better intensity generation
        // either move everything down when hitting maximum
        // or add tolerance to intensity changes when section loudnesses are pretty much the same

        for (int i = 0; i < analysis.Sections.Count; i++)
        {
            Section section = analysis.Sections[i];
            bool isLastSection = i >= (args.Analysis.Sections.Count - 1);

            Section? previousSection = i > 0 ? analysis.Sections[i - 1] : null;

            int intensity;
            if (previousIntensity is int pIntensity)
            {
                Debug.Assert(previousSection is not null);
                intensity = pIntensity;

                double intensityComparison = CompareIntensities(section, previousSection);
                if (intensityComparison >= 0)
                    intensity++;
                else
                    intensity--;

                if (section == loudestSection && intensity < (int)maxIntensity)
                {
                    intensity++;
                    previousWasDoubleJumped = true;
                }
                if (previousWasDoubleJumped && !isLastSection)
                    intensity--;
            }
            else
            {
                Debug.Assert(previousSection is null);

                intensity = (int)IntensityFromFeatures(features);

                // if section is less intensive than features, decrement intensity if possible
                // we do this to be extra conservative with the first section's intensity
                // as it determines the rest of the song's intensity and usually is one of the calmer parts of the song.
                if (CompareIntensities(section, features) < 0)
                    intensity--;
                if (section.Loudness < -15)
                    intensity--;
            }

            yield return (ClampIntensity(intensity), section);
            previousIntensity = intensity;
        }
    }

    /// <summary>
    /// Compute an intensity value (in the range of -1 to 1) for a in relation to b.
    /// </summary>
    private static double CompareIntensities(IntensityComparison a, IntensityComparison b)
    {
        //static double DirectComparison(double a, double b)
        //{
        //    ArgumentOutOfRangeException.ThrowIfLessThan(a, 0, nameof(a));
        //    ArgumentOutOfRangeException.ThrowIfLessThan(b, 0, nameof(b));

        //    return ((a / b) - 1d).Clamp(-1, 1); // clamp since x / 0 => infinity
        //}

        static double NegativeComparison(double a, double b)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(a, 0, nameof(a));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(b, 0, nameof(b));

            return (b / a) - 1d;
        }

        //// when a.Tempo == b.Tempo => (1d) - 1d => 0d
        //// when a.Tempo > b.Tempo => for example (2d) - 1d => 1d;
        //double[] refCollection = [
        //    DirectComparison(a.Tempo, b.Tempo),
        //    NegativeComparison(a.Loudness, b.Loudness)
        //];
        //
        //return refCollection.Sum() / refCollection.Length;

        // there are parts where Tempo == 0 so if b = 0 => a / b => infinite => 1 which isn't ideal
        // as usually these parts aren't even more intensive than a.
        return NegativeComparison(a.Loudness, b.Loudness);
    }

    public static EffectIntensity IntensityFromFeatures(TrackAudioFeatures features)
    {
        // range: 0 - 4 where 4 is very rare
        int intensityRef = (int)(features.Energy * 4f);

        // range: 1 - 5 where 5 is very rare
        int intensity = intensityRef + 1;

        // EffectIntensity.VeryHigh is very rare
        return (EffectIntensity)intensity;
    }
}
