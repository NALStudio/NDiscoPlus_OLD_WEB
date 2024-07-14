using HueApi.ColorConverters;
using HueApi.Models;

namespace NDiscoPlus.Shared.Models;
public readonly struct NDPLight
{
    public NDPLight(LightId id, HuePosition position)
    {
        Id = id;
        Position = position;
    }

    public LightId Id { get; init; }
    public HuePosition Position { get; init; }
}