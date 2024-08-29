using HueApi.Models;
using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.StrobeAnalyzers;
using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.Strobes;

/// <summary>
/// Render strobes when multiple short (and approximately equal length) strobes are detected.
/// </summary>
internal class SegmentBurstStrobes : NDPStrobe
{
    const int MinSegmentCount = 4;

    public override void Generate(StrobeContext ctx, EffectAPI api)
    {
        List<Segment> penis = ctx.
    }
}
