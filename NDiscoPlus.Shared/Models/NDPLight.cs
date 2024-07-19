using HueApi.ColorConverters;
using HueApi.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;
public readonly struct NDPLight
{
    public NDPLight(LightId id, HuePosition position, ColorGamut colorGamut)
    {
        Id = id;
        Position = position;
        ColorGamut = colorGamut;
    }

    [JsonConverter(typeof(JsonLightIdConverter))]
    public LightId Id { get; init; }
    public HuePosition Position { get; init; }
    public ColorGamut ColorGamut { get; init; }
}