using HueApi.ColorConverters;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Helpers;
internal static class ColorHelpers
{
    public static RGBColor ToHueColor(this SKColor color) => new(color.Red, color.Green, color.Blue);
}
