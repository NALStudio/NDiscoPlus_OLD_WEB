using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;
public class NDPColorPalette : IReadOnlyList<SKColor>
{
    private SKColor[] colors;

    public int Count => colors.Length;

    public SKColor this[int index] => colors[index];

    public NDPColorPalette(IEnumerable<SKColor> colors)
    {
        this.colors = colors.ToArray();
    }

    public NDPColorPalette(params SKColor[] colors)
    {
        this.colors = colors;
    }

    public SKColor[] Colors
    {
        get
        {
            int count = this.colors.Length;
            SKColor[] colors = new SKColor[count];
            Array.Copy(this.colors, colors, count);
            return colors;
        }
    }
    public string[] HtmlColors => colors.Select((c) => $"#{c.Red:x2}{c.Green:x2}{c.Blue:x2}").ToArray();

    public IEnumerator<SKColor> GetEnumerator() => ((IEnumerable<SKColor>)colors).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => colors.GetEnumerator();
}

