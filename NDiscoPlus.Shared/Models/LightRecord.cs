using MemoryPack;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
public partial class LightRecord
{
    public static readonly LightRecord Default = new(null);
    public static LightRecord CreateDefault(NDPLight light)
        => new(light);

    [MemoryPackInclude]
    private readonly NDPLight? light;
    [MemoryPackIgnore]
    public NDPLight Light => light ?? throw new InvalidOperationException("Default configuration does not hold a reference to any lights.");

    public Channel Channel { get; init; } = Channel.All;
    public double Brightness { get; init; } = 1d;

    public LightRecord(NDPLight light) : this((NDPLight?)light)
    {
    }

    [MemoryPackConstructor]
    private LightRecord(NDPLight? light)
    {
        this.light = light;
    }
}