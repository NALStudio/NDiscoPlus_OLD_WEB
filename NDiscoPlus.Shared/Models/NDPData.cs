using SkiaSharp;
using SpotifyAPI.Web;
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

public class NDPData
{
    public NDPData(SpotifyPlayerTrack track, NDPContext context, NDPColorPalette palette, TrackAudioAnalysis tempAnalysis)
    {
        Track = track;
        Context = context;
        Palette = palette;
        TempAnalysis = tempAnalysis;
    }

    [JsonRequired]
    public SpotifyPlayerTrack Track { get; init; }

    [JsonRequired]
    public NDPContext Context { get; init; }

    [JsonRequired, JsonConverter(typeof(ColorPaletteConverter))]
    public NDPColorPalette Palette { get; init; }

    [JsonRequired]
    public TrackAudioAnalysis TempAnalysis { get; init; }

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
    public override NDPColorPalette? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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