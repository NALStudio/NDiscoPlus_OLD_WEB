using NDiscoPlus.Shared.Models.Color;
using System.Diagnostics;

namespace NDiscoPlus.Models;

public readonly record struct BackgroundGradientColor(double X, double Y, NDPColor Color)
{
    public double[] Serialize()
    {
        return [
            X, Y,
            Color.X, Color.Y, Color.Brightness
        ];
    }
}