using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using NDiscoPlus.Shared;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Music;
using SpotifyAPI.Web;

namespace NDiscoPlus.Components;

public class TrackDebugCanvasRenderMeta : TrackDebugCanvasRender
{
    public TrackDebugCanvasRenderMeta(Context2D canvas, int canvasWidth, int canvasHeight, SpotifyPlayerContext context, TrackAudioFeatures features, TrackAudioAnalysis analysis) : base(canvas, canvasWidth, canvasHeight, context, features, analysis)
    {
    }

    public override async Task RenderAsync()
    {
        int trackHeight = 50;
        int trackY = canvasHeight - trackHeight;

        int sectionsHeight = 100;
        int sectionsY = trackY - sectionsHeight;

        int segmentsY = 0;
        int segmentsHeight = sectionsY - segmentsY;

        await RenderTrack(y: trackY, height: trackHeight);
        await RenderSections(y: sectionsY, height: sectionsHeight);
        await RenderPlayer();
    }

    double Transform(TimeSpan time)
        => Transform(time.TotalSeconds);

    double Transform(double time)
    {
        double length = player.Track.Length.TotalSeconds;

        double progress = time / length;

        return progress * canvasWidth;
    }

    async Task RenderTrack(int y, int height)
    {
        EffectIntensity intensity = MusicEffectGenerator.IntensityFromFeatures(features);

        await RenderIntensity(intensity, 0d, y, canvasWidth, height);
        // keep using RenderIntensity's text color

        string[] data = ["Track Features:", $"{features.Loudness} dB", $"{features.Tempo} BPM"];

        double textY = 0d;
        foreach (string d in data)
        {
            TextMetrics metrics = await canvas.MeasureTextAsync(d);

            double fontHeight = metrics.FontBoundingBoxAscent + metrics.FontBoundingBoxDescent;
            textY += fontHeight;

            await canvas.FillTextAsync(d, 0d, y + textY);
        }
    }

    async Task RenderSections(int y, int height)
    {
        IReadOnlyList<ComputedIntensity> intensities = MusicEffectGenerator.ComputeIntensities(new NDiscoPlusArgs(player.Track, features, analysis, new NDiscoPlusArgsLights()));

        for (int i = 0; i < intensities.Count; i++)
        {
            (EffectIntensity intensity, Section section) = intensities[i];

            double x = Transform(section.Start);
            double width = Transform(section.Duration);

            await RenderIntensity(intensity, x, y, width, height, centerTextVertically: false);
            // keep using RenderIntensity's text color

            string[] data = [$"Section {i}:", $"{section.Loudness} dB", $"{section.Tempo} BPM", $"{section.Duration:.000} seconds"];

            double textY = 0d;
            foreach (string d in data)
            {
                TextMetrics metrics = await canvas.MeasureTextAsync(d);

                double fontHeight = metrics.FontBoundingBoxAscent + metrics.FontBoundingBoxDescent;
                textY += fontHeight;

                await canvas.FillTextAsync(d, x, y + textY);
            }
        }
    }

    async Task RenderIntensity(EffectIntensity intensity, double x, double y, double width, double height, bool centerTextVertically = true)
    {
        EffectIntensity[] allIntensities = Enum.GetValues<EffectIntensity>();
        EffectIntensity minIntensity = allIntensities.Min();
        EffectIntensity maxIntensity = allIntensities.Max();

        double hue = DoubleHelpers.Remap((int)intensity, (int)minIntensity, (int)maxIntensity, 0d, 120d);
        string hsl50 = $"hsl({(int)hue}, 100%, 50%)";
        string hsl35 = $"hsl({(int)hue}, 100%, 35%)";

        await canvas.FillStyleAsync(hsl50);
        await canvas.LineWidthAsync(1);
        await canvas.StrokeStyleAsync(hsl35);

        await canvas.FillRectAsync(x, y, width, height);
        await canvas.StrokeRectAsync(x, y, width, height);

        await canvas.FillStyleAsync(hsl35);

        string intensityText = $"{intensity:G} ({intensity:D})";

        TextMetrics metrics = await canvas.MeasureTextAsync(intensityText);

        double textY = height - metrics.FontBoundingBoxDescent;
        if (centerTextVertically)
            textY /= 2d;

        // clipping
        await canvas.SaveAsync();
        await canvas.BeginPathAsync();
        await canvas.RectAsync(x, y, width, height);
        await canvas.ClipAsync(FillRule.NonZero);

        // text
        await canvas.FillTextAsync(intensityText, x + ((width - metrics.Width) / 2d), y + textY);

        // remove clipping
        await canvas.RestoreAsync();
    }

    async Task RenderPlayer()
    {
        double pos = Transform(player.Progress);

        await canvas.StrokeStyleAsync("#000000");
        await canvas.LineWidthAsync(2);

        await canvas.BeginPathAsync();
        await canvas.MoveToAsync(pos, 0d);
        await canvas.LineToAsync(pos, canvasHeight);
        await canvas.StrokeAsync();
    }
}
