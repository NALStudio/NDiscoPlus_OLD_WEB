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
    public static RGBColor ToHueColor(this SKColor color) => new((int)color.Red, (int)color.Green, (int)color.Blue);

    public static SKColor ToSKColor(this RGBColor color)
    {
        byte red = (byte)(color.R * 255.99);
        byte green = (byte)(color.G * 255.99);
        byte blue = (byte)(color.B * 255.99);
        return new SKColor(red, green, blue);
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

    /// <summary>
    /// Computes a gradient color with mix (same as t in Lerp). This takes gamma into account.
    /// </summary>
    public static RGBColor Gradient(RGBColor color1, RGBColor color2, double mix)
    {
        static double sRGBInverseCompanding(double x)
        {
            if (x <= 0.04045d)
                return x / 12.92d;
            else
                return Math.Pow((x + 0.055d) / 1.055d, 2.4d);
        }

        static double sRGBCompanding(double x)
        {
            if (x <= 0.0031308d)
                return x * 12.92d;
            else
                return (1.055d * Math.Pow(x, 1d / 2.4d)) - 0.055d;
        }

        double r1 = sRGBInverseCompanding(color1.R);
        double g1 = sRGBInverseCompanding(color1.G);
        double b1 = sRGBInverseCompanding(color1.B);

        double r2 = sRGBInverseCompanding(color2.R);
        double g2 = sRGBInverseCompanding(color2.G);
        double b2 = sRGBInverseCompanding(color2.B);

        double r = DoubleHelpers.Lerp(r1, r2, mix);
        double g = DoubleHelpers.Lerp(g1, g2, mix);
        double b = DoubleHelpers.Lerp(b1, b2, mix);

        const double gamma = 0.43;
        double brightness1 = Math.Pow(r1 + g1 + b1, gamma);
        double brightness2 = Math.Pow(r2 + g2 + b2, gamma);

        double brightness = DoubleHelpers.Lerp(brightness1, brightness2, mix);
        double intensity = Math.Pow(brightness, 1 / gamma);

        double rgbSum = r + g + b;
        if (rgbSum != 0)
        {
            double factor = intensity / rgbSum;
            r *= factor;
            g *= factor;
            b *= factor;
        }

        return new RGBColor(
            sRGBCompanding(r),
            sRGBCompanding(g),
            sRGBCompanding(b)
        );
    }
}
