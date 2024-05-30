using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;

public class NDiscoPlusData
{
    public NDiscoPlusData(SpotifyPlayerTrack track, NDiscoPlusContext context, NDiscoPlusColorPalette palette)
    {
        Track = track;
        Context = context;
        Palette = palette;
    }

    [JsonRequired]
    public SpotifyPlayerTrack Track { get; init; }

    [JsonRequired]
    public NDiscoPlusContext Context { get; init; }

    [JsonRequired, JsonConverter(typeof(ColorPaletteConverter))]
    public NDiscoPlusColorPalette Palette { get; init; }

    public static string Serialize(NDiscoPlusData data)
    {
        string output = JsonSerializer.Serialize(data);
        Debug.Assert(!string.IsNullOrEmpty(output));
        return output;
    }

    public static NDiscoPlusData Deserialize(string data)
    {
        NDiscoPlusData? d = JsonSerializer.Deserialize<NDiscoPlusData>(data);
        return d ?? throw new InvalidOperationException("Cannot deserialize value.");
    }
}

class ColorPaletteConverter : JsonConverter<NDiscoPlusColorPalette>
{
    public override NDiscoPlusColorPalette? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        reader.Read();

        List<SKColor> colors = new();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            colors.Add(SKColor.Parse(reader.GetString()));
            reader.Read();
        }

        return new NDiscoPlusColorPalette(colors);
    }

    public override void Write(Utf8JsonWriter writer, NDiscoPlusColorPalette value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (SKColor color in value.Colors)
            writer.WriteStringValue(color.ToString());

        writer.WriteEndArray();
    }
}