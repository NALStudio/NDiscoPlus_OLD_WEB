using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.Effect;

internal abstract class EffectState;

internal abstract class NDPEffect<T> where T : EffectState
{
    public static ImmutableList<NDPEffect<EffectState>> All = [

    ];

    /// <param name="name"></param>
    /// <param name="intensity">Describes the intensity of this effect. 1 (lowest) - 5 (highest)</param>
    /// <param name="type"></param>
    protected NDPEffect(string name, byte intensity, EffectType type)
    {
        Name = name;
        Intensity = intensity;
        Type = type;
    }

    public string Name { get; }
    public byte Intensity { get; }

    public abstract T CreateState();
}

