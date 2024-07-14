using HueApi.ColorConverters;
using NDiscoPlus.Shared.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;
public readonly struct NDPColor
{
    public double X { get; }
    public double Y { get; }
    public double Brightness { get; }

    public NDPColor(double x, double y, double brightness)
    {
        X = x;
        Y = y;
        Brightness = brightness;
    }

    // https://en.wikipedia.org/wiki/SRGB#From_sRGB_to_CIE_XYZ
    public static NDPColor FromLinearRGB(double r, double g, double b)
    {
        double x = (0.4124d * r) + (0.3576d * g) + (0.1805d * b);
        double y = (0.2126d * r) + (0.7152d * g) + (0.0722d * b);
        double z = (0.0193d * r) + (0.1192d * g) + (0.9505d * b);

        return FromXYZ(x, y, z);
    }

    public static NDPColor FromSRGB(double r, double g, double b)
    {
        r = ColorHelpers.SRGBInverseCompanding(r);
        g = ColorHelpers.SRGBInverseCompanding(g);
        b = ColorHelpers.SRGBInverseCompanding(b);

        return FromLinearRGB(r, g, b);
    }

    // https://developers.meethue.com/develop/application-design-guidance/color-conversion-formulas-rgb-to-xy-and-back/#Color-rgb-to-xy
    public static NDPColor FromXYZ(double x, double y, double z)
    {
        double sum = x + y + z;

        return new(
            x: x / sum,
            y: y / sum,
            brightness: y
        );
    }

    // https://en.wikipedia.org/wiki/SRGB#From_CIE_XYZ_to_sRGB
    public (double R, double G, double B) ToLinearRGB()
    {
        (double x, double y, double z) = ToXYZ();

        double r = (3.2406d * x) + (-1.5372d * y) + (-0.4986d * z);
        double g = (-0.9689d * x) + (1.8758d * y) + (0.0415d * z);
        double b = (0.0557d * x) + (-0.2040d * y) + (1.0570d * z);

        return (r, g, b);
    }

    public (double R, double G, double B) ToSRGB()
    {
        (double r, double g, double b) = ToLinearRGB();

        r = ColorHelpers.SRGBCompanding(r);
        g = ColorHelpers.SRGBCompanding(g);
        b = ColorHelpers.SRGBCompanding(b);

        return (r, g, b);
    }

    // https://developers.meethue.com/develop/application-design-guidance/color-conversion-formulas-rgb-to-xy-and-back/#xy-to-rgb-color
    public (double X, double Y, double Z) ToXYZ()
    {
        double x = X;
        double y = Y;
        double z = 1d - x - y;

        return (
            X: Y / y * x,
            Y: Brightness,
            Z: Y / y * z
        );
    }

    public NDPColor Clamp(ColorGamut gamut)
    {
        ColorGamutPoint p = new(X, Y);

        if (gamut.ContainsPoint(p))
            return this;

        ColorGamutPoint closest = gamut.GetClosestPoint(p);
        return new NDPColor(x: closest.X, y: closest.Y, brightness: Brightness);
    }

    public static NDPColor Lerp(NDPColor c1, NDPColor c2, double t)
    {
        t = DoubleHelpers.Clamp01(t);

        return new(
            x: DoubleHelpers.LerpUnclamped(c1.X, c2.X, t),
            y: DoubleHelpers.LerpUnclamped(c1.Y, c2.Y, t),
            brightness: DoubleHelpers.LerpUnclamped(c1.Brightness, c2.Brightness, t)
        );
    }

    // In the second answer after Mark's answer, there is a comparison with xyY color mixing
    // and that looked good, so I'll be using that instead.
    // public static NDPColor Gradient(NDPColor c1, NDPColor c2, double t)
    // {
    //     // Stolen from: https://stackoverflow.com/questions/22607043/color-gradient-algorithm/49321304#49321304

    //     (double r1, double g1, double b1) = c1.ToLinearRGB();
    //     (double r2, double g2, double b2) = c2.ToLinearRGB();

    //     double r = DoubleHelpers.Lerp(r1, r2, t);
    //     double g = DoubleHelpers.Lerp(g1, g2, t);
    //     double b = DoubleHelpers.Lerp(b1, b2, t);

    //     const double gamma = 0.43;
    //     double brightness1 = Math.Pow(r1 + g1 + b1, gamma);
    //     double brightness2 = Math.Pow(r2 + g2 + b2, gamma);

    //     double brightness = DoubleHelpers.Lerp(brightness1, brightness2, t);
    //     double intensity = Math.Pow(brightness, 1 / gamma);

    //     double rgbSum = r + g + b;
    //     if (rgbSum != 0)
    //     {
    //         double factor = intensity / rgbSum;
    //         r *= factor;
    //         g *= factor;
    //         b *= factor;
    //     }

    //     return FromLinearRGB(r, g, b);
    // }
}