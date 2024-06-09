using NDiscoPlus.Shared.Models;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Effects.Effect;

internal abstract class EffectState;



internal abstract class NDPEffect<T> where T : EffectState
{
    public static readonly ImmutableList<NDPEffect<EffectState>> All = [

    ];

    public static readonly IDictionary<EffectIntensity, IList<NDPEffect<EffectState>>> ByIntensity = All.GroupBy(e => e.Intensity).ToFrozenDictionary(x => x.Key, x => (IList<NDPEffect<EffectState>>)x.ToImmutableArray());

    /// <summary>An effect used by NDiscoPlus</summary>
    /// <param name="name">The name of the effect.</param>
    /// <param name="intensity">Describes the intensity of this effect. 1 (lowest) - 5 (highest)</param>
    protected NDPEffect(string name, EffectIntensity intensity)
    {
        Name = name;
        Intensity = intensity;
    }

    public string Name { get; }
    public EffectIntensity Intensity { get; }


}