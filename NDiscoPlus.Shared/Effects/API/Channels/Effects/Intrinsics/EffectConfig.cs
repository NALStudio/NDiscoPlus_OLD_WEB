using MemoryPack;
using NDiscoPlus.Shared.Models.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
[MemoryPackable]
public partial class EffectConfig
{
    public enum StrobeStyles { Instant, Realistic }

    public double BaseBrightness { get; init; } = 0.1d;
    public double MaxBrightness { get; init; } = 1d;

    public double StrobeCCT { get; init; } = 5000;
    public StrobeStyles StrobeStyle { get; } = StrobeStyles.Instant;

    [MemoryPackIgnore]
    public NDPColor StrobeColor => NDPColor.FromCCT(StrobeCCT);
}