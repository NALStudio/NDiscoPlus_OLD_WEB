using HueApi.ColorConverters;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Helpers;
public static class ColorHelpers
{
    private static readonly double rgbCnvMult = Math.BitDecrement(256d);

    public static string ToHTMLColor(double r, double g, double b, double alpha = 1d)
    {
        byte red = (byte)(r * rgbCnvMult);
        byte green = (byte)(g * rgbCnvMult);
        byte blue = (byte)(b * rgbCnvMult);
        byte alpha_ = (byte)(alpha * rgbCnvMult);

        return $"#{red:x2}{green:x2}{blue:x2}{alpha_:x2}";
    }

    public static RGBColor Lerp(RGBColor a, RGBColor b, double t)
    {
        t = Math.Clamp(t, 0d, 1d);

        return new RGBColor(
            a.R + ((b.R - a.R) * t),
            a.G + ((b.G - a.G) * t),
            a.B + ((b.B - a.B) * t)
        );
    }

    // https://en.wikipedia.org/wiki/SRGB#From_sRGB_to_CIE_XYZ
    public static double SRGBInverseCompanding(double c)
    {
        if (c <= 0.04045d)
            return c / 12.92d;
        else
            return Math.Pow((c + 0.055d) / 1.055d, 2.4d);
    }

    // https://en.wikipedia.org/wiki/SRGB#From_CIE_XYZ_to_sRGB
    public static double SRGBCompanding(double c)
    {
        if (c <= 0.0031308d)
            return c * 12.92d;
        else
            return (1.055d * Math.Pow(c, 1d / 2.4d)) - 0.055d;
    }
}
