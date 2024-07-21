using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models.Color;
using SkiaSharp;
using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;
public readonly struct NDPColorPalette : IReadOnlyList<NDPColor>
{
    private readonly ImmutableArray<NDPColor> colors;

    public readonly int Count => colors.Length;

    public NDPColor this[int index] => colors[index];

    public NDPColorPalette(IEnumerable<NDPColor> colors)
    {
        this.colors = colors.ToImmutableArray();
    }

    public NDPColorPalette(params NDPColor[] colors)
    {
        this.colors = colors.ToImmutableArray();
    }

    public NDPColorPalette(ImmutableArray<NDPColor> colors)
    {
        this.colors = colors;
    }

    public NDPColorPalette(IEnumerable<SKColor> colors)
    {
        this.colors = colors.Select(c => NDPColor.FromSRGB(c.Red / 255d, c.Green / 255d, c.Blue / 255d)).ToImmutableArray();
    }

    public NDPColorPalette(params SKColor[] colors) : this((IEnumerable<SKColor>)colors)
    { }

    public readonly IList<NDPColor> Colors => colors;


    public string[] HtmlColors => GetHtmlColors().ToArray();
    private IEnumerable<string> GetHtmlColors()
    {
        foreach (NDPColor c in colors)
        {
            (double r, double g, double b) = c.ToSRGB();
            yield return ColorHelpers.ToHTMLColor(r, g, b);
        }
    }

    public IEnumerator<NDPColor> GetEnumerator() => ((IEnumerable<NDPColor>)colors).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class JsonNDPColorPaletteConverter : JsonConverter<NDPColorPalette>
{
    public override NDPColorPalette Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ImmutableArray<NDPColor> colors = JsonSerializer.Deserialize<ImmutableArray<NDPColor>>(ref reader, options);
        return new NDPColorPalette(colors);
    }

    public override void Write(Utf8JsonWriter writer, NDPColorPalette value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (IReadOnlyList<NDPColor>)value, options);
    }
}