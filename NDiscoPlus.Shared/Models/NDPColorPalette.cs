using HueApi.ColorConverters;
using NDiscoPlus.Shared.Helpers;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;
public readonly struct NDPColorPalette : IReadOnlyList<SKColor>
{
    private readonly ImmutableArray<SKColor> colors;

    public readonly int Count => colors.Length;

    public SKColor this[int index] => colors[index];

    public NDPColorPalette(IEnumerable<SKColor> colors)
    {
        this.colors = colors.ToImmutableArray();
    }

    public NDPColorPalette(params SKColor[] colors)
    {
        this.colors = colors.ToImmutableArray();
    }

    public readonly IList<SKColor> Colors => colors;

    public readonly RGBColor[] HueColors => colors.Select(c => c.ToHueColor()).ToArray();

    public string[] HtmlColors => colors.Select((c) => $"#{c.Red:x2}{c.Green:x2}{c.Blue:x2}").ToArray();

    public IEnumerator<SKColor> GetEnumerator() => ((IEnumerable<SKColor>)colors).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}