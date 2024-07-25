using HueApi.ColorConverters;
using NDiscoPlus.Shared.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models.Color;
public readonly partial struct NDPColor : IEquatable<NDPColor>
{
    public double X { get; }
    public double Y { get; }
    public double Brightness { get; }

    [JsonConstructor]
    public NDPColor(double x, double y, double brightness)
    {
        X = x;
        Y = y;
        Brightness = brightness;
    }

    public NDPColor CopyWith(double? x = null, double? y = null, double? brightness = null)
        => new(x ?? X, y ?? Y, brightness ?? Brightness);

    public override bool Equals(object? obj)
    {
        return obj is NDPColor color && Equals(color);
    }

    public bool Equals(NDPColor other)
    {
        return X == other.X &&
               Y == other.Y &&
               Brightness == other.Brightness;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Brightness);
    }

    public static bool operator ==(NDPColor left, NDPColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NDPColor left, NDPColor right)
    {
        return !(left == right);
    }
}