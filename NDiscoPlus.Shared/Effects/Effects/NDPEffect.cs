using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Models;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Effects.Effects;

internal abstract class NDPEffect
{
    public static readonly ImmutableList<NDPEffect> All = [
        BrightLightEffect.Default(EffectIntensity.High),
        BrightLightEffect.Slow(EffectIntensity.VeryLow),
        BrightLightEffect.White(EffectIntensity.VeryHigh),
    ];

    public static readonly IDictionary<EffectIntensity, IList<NDPEffect>> ByIntensity = Enum.GetValues<EffectIntensity>()
        .Select(i => new KeyValuePair<EffectIntensity, IList<NDPEffect>>(i, All.Where(eff => eff.Intensity == i).ToImmutableList()))
        .ToImmutableDictionary();

    public abstract EffectState CreateState(StateContext ctx);
    public abstract void Update(EffectContext ctx, EffectState effectState);


    /// <summary>An effect specialised in categorisation by intensity.</summary>
    /// <param name="intensity">Describes the intensity of this effect. 1 (lowest) - 5 (highest)</param>
    protected NDPEffect(EffectIntensity intensity)
    {
        Intensity = intensity;
    }

    public EffectIntensity Intensity { get; }
}