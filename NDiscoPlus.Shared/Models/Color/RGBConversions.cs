using NDiscoPlus.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models.Color;
public readonly partial struct NDPColor
{
    // https://en.wikipedia.org/wiki/SRGB#From_sRGB_to_CIE_XYZ
    public static NDPColor FromLinearRGB(double r, double g, double b)
    {
        if (r < 0d || r > 1d)
            throw new ArgumentOutOfRangeException(nameof(r));
        if (g < 0d || g > 1d)
            throw new ArgumentOutOfRangeException(nameof(g));
        if (b < 0d || b > 1d)
            throw new ArgumentOutOfRangeException(nameof(b));

        double x = 0.4124d * r + 0.3576d * g + 0.1805d * b;
        double y = 0.2126d * r + 0.7152d * g + 0.0722d * b;
        double z = 0.0193d * r + 0.1192d * g + 0.9505d * b;

        return FromXYZ(x, y, z);
    }

    public static NDPColor FromSRGB(double r, double g, double b)
    {
        if (r < 0d || r > 1d)
            throw new ArgumentOutOfRangeException(nameof(r));
        if (g < 0d || g > 1d)
            throw new ArgumentOutOfRangeException(nameof(g));
        if (b < 0d || b > 1d)
            throw new ArgumentOutOfRangeException(nameof(b));

        r = ColorHelpers.SRGBInverseCompanding(r);
        g = ColorHelpers.SRGBInverseCompanding(g);
        b = ColorHelpers.SRGBInverseCompanding(b);

        return FromLinearRGB(r, g, b);
    }



    // https://en.wikipedia.org/wiki/SRGB#From_CIE_XYZ_to_sRGB
    public (double R, double G, double B) ToLinearRGB()
    {
        (double x, double y, double z) = ToXYZ();

        double r = 3.2406d * x + -1.5372d * y + -0.4986d * z;
        double g = -0.9689d * x + 1.8758d * y + 0.0415d * z;
        double b = 0.0557d * x + -0.2040d * y + 1.0570d * z;

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
}
