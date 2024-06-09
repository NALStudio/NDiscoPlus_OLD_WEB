using HueApi.ColorConverters;
using HueApi.ColorConverters.Original.Extensions;
using HueApi.Models;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using SkiaSharp;
using SpotifyAPI.Web;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Music;

public class LightInterpreterConfig
{
    public double BackgroundRPM { get; init; } = 5d;

    public double BaseBrightness { get; init; } = 0.3d;
    public double MaxBrightness { get; init; } = 1d;
}

public class LightInterpreter
{
    public LightInterpreterConfig Config { get; }
    public NDPLightCollection Lights { get; }

    public string? trackId;

    public int barIndex;
    public int beatIndex;
    public int tatumIndex;

    void Reset()
    {
        barIndex = -1;
        beatIndex = -1;
        tatumIndex = -1;
    }

    public LightInterpreter(LightInterpreterConfig config, params NDPLight[] lights)
    {
        Config = config;
        Lights = NDPLightCollection.Create(lights);
    }

    private static int FindBeatIndex(TimeSpan progress, IList<NDPInterval> timings, int previousIndex)
    {
        if (previousIndex >= 0 && previousIndex < timings.Count)
        {
            // if previous index in range, check next beat in the future first where it most likely resorts (if we haven't seeked)

            if (timings[previousIndex].Contains(progress))
                return previousIndex;

            for (int i = (previousIndex + 1); i < timings.Count; i++)
            {
                if (timings[i].Contains(progress))
                    return i;
            }

            for (int i = (previousIndex - 1); i >= 0; i--)
            {
                if (timings[i].Contains(progress))
                    return i;
            }

            return -1;
        }
        else
        {
            // if previous index isn't in range, the new track is either shorter than the previous track or there isn't a previous track yet
            // in this case just do a linear search.

            for (int i = 0; i < timings.Count; i++)
            {
                if (timings[i].Contains(progress))
                    return i;
            }

            return -1;
        }
    }

    private static (int BeatIndex, bool BeatChanged) UpdateBeats(TimeSpan progress, IList<NDPInterval> timings, int previousIndex)
    {
        int newIndex = FindBeatIndex(progress, timings, previousIndex);
        return (newIndex, newIndex != previousIndex);
    }

    public IReadOnlyList<NDPLight> Update(TimeSpan progress, NDPData data)
    {
        if (trackId != data.Track.Id)
        {
            trackId = data.Track.Id;
            Reset();
        }

        // data update
        (barIndex, bool newBar) = UpdateBeats(progress, data.Timings.Bars, barIndex);
        (beatIndex, bool newBeat) = UpdateBeats(progress, data.Timings.Beats, beatIndex);
        (tatumIndex, bool newTatum) = UpdateBeats(progress, data.Timings.Tatums, tatumIndex);

        // light update
        UpdateLightsBase(progress, data.EffectPalette);

        return Lights;
    }

    private void UpdateLightsBase(TimeSpan progress, NDPColorPalette palette)
    {
        double xSize = Lights.Bounds.MaxX - Lights.Bounds.MinX;
        double ySize = Lights.Bounds.MaxY - Lights.Bounds.MinY;

        (NDPLight Light, double NormalizedPosition)[] lightAxis;
        if (xSize >= ySize)
            lightAxis = Lights.Select(l => (l, l.Position.X.Remap01(Lights.Bounds.MinX, Lights.Bounds.MaxX))).ToArray();
        else
            lightAxis = Lights.Select(l => (l, l.Position.Y.Remap01(Lights.Bounds.MinY, Lights.Bounds.MaxY))).ToArray();

        RGBColor[] huePalette = palette.Select(c => c.ToHueColor()).ToArray();

        double backgroundRotationProgress = (progress.TotalMinutes * Config.BackgroundRPM) % 1d;

        // TODO: Find a better way to show the palette without CSS gradient being ugly
        // Probably just transition each light on its own to a new random color once in a while
        foreach ((NDPLight light, double normalizedPosition) in lightAxis)
        {
            double paletteProgress = backgroundRotationProgress * palette.Count;
            double paletteT = normalizedPosition + paletteProgress;

            int gradientIndex = (int)paletteT;
            double gradientT = paletteT - gradientIndex;

            RGBColor c1 = huePalette[gradientIndex % huePalette.Length];
            RGBColor c2 = huePalette[(gradientIndex + 1) % huePalette.Length];

            light.Color = ColorHelpers.Gradient(c1, c2, gradientT);
            light.Brightness = Config.MaxBrightness;
        }
    }
}
