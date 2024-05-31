using HueApi.ColorConverters;
using HueApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;
public record NDPLight
{
    public NDPLight(byte? id, HuePosition position)
    {
        Id = id;
        Position = position;
        Color = new RGBColor(1d, 1d, 1d);
        Brightness = 1d;
    }

    public byte? Id { get; init; }
    public HuePosition Position { get; init; }

    public RGBColor Color { get; internal set; }
    public double Brightness { get; internal set; }
}