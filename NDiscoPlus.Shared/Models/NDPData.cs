using NDiscoPlus.Shared.Music;
using SkiaSharp;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

public class NDPData
{
    public NDPData(SpotifyPlayerTrack track, NDPContext context, NDPColorPalette referencePalette, NDPColorPalette effectPalette, NDPTimings timings, IList<EffectRecord> effects)
    {
        Track = track;
        Context = context;
        ReferencePalette = referencePalette;
        EffectPalette = effectPalette;
        Timings = timings;
        Effects = effects.ToImmutableArray();
    }

    public SpotifyPlayerTrack Track { get; }

    public NDPContext Context { get; }

    public NDPColorPalette ReferencePalette { get; }
    public NDPColorPalette EffectPalette { get; }

    public NDPTimings Timings { get; }

    public IList<EffectRecord> Effects { get; }

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
