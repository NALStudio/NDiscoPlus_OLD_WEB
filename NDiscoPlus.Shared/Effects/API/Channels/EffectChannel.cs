using HueApi.ColorConverters;
using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.API.Channels;
public abstract class EffectChannel
{
    IList<NDPLight> Lights => lights;

    readonly ImmutableArray<NDPLight> lights;

    protected EffectChannel(params NDPLight[] lights)
    {
        this.lights = lights.ToImmutableArray();
    }
}
