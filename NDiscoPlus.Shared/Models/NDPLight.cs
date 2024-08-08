using HueApi.ColorConverters;
using HueApi.Models;
using MemoryPack;
using NDiscoPlus.Shared.MemoryPack.Formatters;
using NDiscoPlus.Shared.Models.Color;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
public readonly partial struct NDPLight
{
    public NDPLight(LightId id, HuePosition position, ColorGamut? colorGamut)
    {
        Id = id;
        Position = position;
        ColorGamut = colorGamut;
    }

    public LightId Id { get; init; }

    [HuePositionFormatter]
    public HuePosition Position { get; init; }

    public ColorGamut? ColorGamut { get; init; }
}