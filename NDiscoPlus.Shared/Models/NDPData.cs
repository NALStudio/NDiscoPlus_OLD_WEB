using NDiscoPlus.Shared.Effects.Effect;
using SkiaSharp;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

public record EffectRecord
{
    internal NDPEffect? Effect { get; }
    internal TimeSpan Start { get; }

    internal EffectRecord(NDPEffect? effect, TimeSpan start)
    {
        Effect = effect;
        Start = start;
    }
}

public class NDPData
{
    public NDPData(SpotifyPlayerTrack track, NDPContext context, NDPColorPalette referencePalette, NDPColorPalette effectPalette, NDPTimings timings, IEnumerable<EffectRecord> effects)
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

    [JsonConverter(typeof(ColorPaletteConverter))]
    public NDPColorPalette ReferencePalette { get; }

    [JsonConverter(typeof(ColorPaletteConverter))]
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

class ColorPaletteConverter : JsonConverter<NDPColorPalette>
{
    public override NDPColorPalette Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        reader.Read();

        List<SKColor> colors = [];

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            colors.Add(SKColor.Parse(reader.GetString()));
            reader.Read();
        }

        return new NDPColorPalette(colors);
    }

    public override void Write(Utf8JsonWriter writer, NDPColorPalette value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (SKColor color in value.Colors)
            writer.WriteStringValue(color.ToString());

        writer.WriteEndArray();
    }
}