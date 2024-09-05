﻿using MemoryPack;
using NDiscoPlus.Shared.MemoryPack.Formatters;
using NDiscoPlus.Shared.Models.Color;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
public readonly partial struct LightPosition
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public LightPosition(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

[MemoryPackable]
public readonly partial struct NDPLight
{
    public NDPLight(LightId id, LightPosition position, ColorGamut? colorGamut)
    {
        Id = id;
        Position = position;
        ColorGamut = colorGamut;
    }

    public LightId Id { get; init; }
    public LightPosition Position { get; init; }
    public ColorGamut? ColorGamut { get; init; }
}