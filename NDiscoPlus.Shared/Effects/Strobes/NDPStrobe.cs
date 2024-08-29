using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.Strobes;
using NDiscoPlus.Shared.Models;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Effects.StrobeAnalyzers;
internal abstract class NDPStrobe
{
    public static readonly ImmutableArray<NDPStrobe> All = [
        new SegmentBurstStrobes()
    ];

    public abstract void Generate(Context ctx, EffectAPI api);
}
