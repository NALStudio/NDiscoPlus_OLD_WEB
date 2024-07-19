using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models.Color;
using SkiaSharp;
using System.Collections;
using System.Collections.Immutable;

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