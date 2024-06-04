using HueApi.ColorConverters;
using HueApi.Models;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using SkiaSharp;
using SpotifyAPI.Web;
using System;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Music;

public class LightInterpreter
{
    public IList<NDPLight> Lights { get; }
    int beatIndex = 0;

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

        double? barProgress = null;
        foreach (TimeInterval bar in data.TempAnalysis.Bars)
        {
            double progressed = progress.TotalSeconds - bar.Start;
            if (progressed >= 0 && progressed <= bar.Duration)
            {
                barProgress = progressed / bar.Duration;
                break;
            }
        }

        double? beatProgress = null;
        for (beatIndex = 0; beatIndex < data.TempAnalysis.Beats.Count; beatIndex++)
        {
            TimeInterval beat = data.TempAnalysis.Beats[beatIndex];
            double progressed = progress.TotalSeconds - beat.Start;
            if (progressed >= 0 && progressed <= beat.Duration)
            {
                beatProgress = progressed / beat.Duration;
                break;
            }
        }

        if (barProgress.HasValue)
        {
            double brightness = 1d - barProgress.Value;
            double colorShift = beatProgress ?? 1d;

            RGBColor color = data.Palette[beatIndex % data.Palette.Count].ToHueColor();

            foreach (NDPLight light in Lights)
            {
                light.Color = color;
                light.Brightness = brightness;
            }
        }

        return Lights;
    }
}
