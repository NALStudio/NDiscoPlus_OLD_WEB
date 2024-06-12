using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Effects.Effects;
using NDiscoPlus.Shared.Models;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Effects.Effect;

internal abstract class NDPEffect : NDPBaseEffect
{
    public static readonly ImmutableList<NDPEffect> All = [
        BrightLightEffect.Default(),
        BrightLightEffect.Slow(),
        BrightLightEffect.White(),
    ];

    public static readonly IDictionary<EffectIntensity, IList<NDPEffect>> ByIntensity = All.GroupBy(e => e.Intensity).ToFrozenDictionary(x => x.Key, x => (IList<NDPEffect>)x.ToImmutableArray());

    /// <summary>An effect specialised in categorisation by intensity.</summary>
    /// <param name="intensity">Describes the intensity of this effect. 1 (lowest) - 5 (highest)</param>
    protected NDPEffect(EffectIntensity intensity)
    {
        Intensity = intensity;
    }

    public EffectIntensity Intensity { get; }
}