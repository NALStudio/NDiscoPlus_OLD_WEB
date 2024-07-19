using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Music;
using SkiaSharp;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

public class NDPData
{
    [JsonConstructor]
    internal NDPData(SpotifyPlayerTrack track, NDPColorPalette referencePalette, NDPColorPalette effectPalette, EffectAPI effects)
    {
        Track = track;
        ReferencePalette = referencePalette;
        EffectPalette = effectPalette;
        Effects = effects;
    }

    public SpotifyPlayerTrack Track { get; }

    public NDPColorPalette ReferencePalette { get; }
    public NDPColorPalette EffectPalette { get; }

    [JsonInclude]
    internal EffectAPI Effects { get; }

    public static string Serialize(NDPData data)
    {
        string output = JsonSerializer.Serialize(data);
        Debug.Assert(!string.IsNullOrEmpty(output));
        return output;
    }

    public static NDPData Deserialize(string data)
    {
        NDPData? d = JsonSerializer.Deserialize<NDPData>(data);
        return d ?? throw new InvalidOperationException("Cannot deserialize value.");
    }
}
