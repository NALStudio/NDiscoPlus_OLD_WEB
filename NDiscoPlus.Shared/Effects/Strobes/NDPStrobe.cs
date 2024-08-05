using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Models;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Effects.StrobeAnalyzers;
internal abstract class NDPStrobe
{
    public static readonly ImmutableList<NDPStrobe> All = [

    ];

    public abstract void Generate(EffectContext ctx, EffectAPI api);
}
