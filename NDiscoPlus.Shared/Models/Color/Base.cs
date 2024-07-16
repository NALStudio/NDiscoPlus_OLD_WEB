using HueApi.ColorConverters;
using NDiscoPlus.Shared.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models.Color;
public readonly partial struct NDPColor
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
}