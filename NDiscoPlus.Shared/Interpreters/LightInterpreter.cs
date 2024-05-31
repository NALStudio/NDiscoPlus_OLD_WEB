using HueApi.ColorConverters;
using HueApi.Models;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using SkiaSharp;
using SpotifyAPI.Web;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Interpreters;

public class LightInterpreter
{

    public IList<NDPLight> Lights { get; }

    public LightInterpreter(params NDPLight[] lights)
    {
        if (lights.Length < 1)
            throw new ArgumentException("At least one light should be provided.", nameof(lights));
        Lights = lights.ToImmutableList();
    }

    public IList<NDPLight> Update(TimeSpan progress, NDPData data)
    {
        for (int i = 0; i < Lights.Count; i++)
        {
            int colorIndex = i % data.Palette.Count;
            Lights[i].Color = data.Palette[colorIndex].ToHueColor();
        }

        foreach (TimeInterval beat in data.TempAnalysis.Beats)
        {
            double progressed = progress.TotalSeconds - beat.Start;
            if (progressed >= 0 && progressed <= beat.Duration)
            {
                double brightness = 1.0 - (progressed / beat.Duration);
                foreach (NDPLight light in Lights)
                {
                    light.Color = new RGBColor(255, 255, 255);
                    light.Brightness = brightness;
                }
                break;
            }
        }

        return Lights;
    }
}
