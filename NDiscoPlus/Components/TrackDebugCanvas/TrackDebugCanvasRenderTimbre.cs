using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using MudBlazor;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Spotify.Models;
using SpotifyAPI.Web;
using System.Collections.Frozen;
using System.Diagnostics;

namespace NDiscoPlus.Components;

[Obsolete("Timbre values should not be compared independently.", true, UrlFormat = "https://github.com/spotify/web-api/issues/947#issuecomment-569582040")]
public class TrackDebugCanvasRenderTimbre : TrackDebugCanvasRender
{
    const double TimbreMaxValue = 150d;

    enum DrawColor { Red, Green, Gray }
    // Use FrozenDictinoary as variable is instantiated only once
    readonly FrozenDictionary<DrawColor, string> ColorToHTML = new Dictionary<DrawColor, string>()
    {
        { DrawColor.Red, "#ff0000" },
        { DrawColor.Green, "#00ff00" },
        { DrawColor.Gray, "#808080" },
    }.ToFrozenDictionary();

    public TrackDebugCanvasRenderTimbre(Context2D canvas, int canvasWidth, int canvasHeight, SpotifyPlayerContext context, TrackAudioFeatures features, TrackAudioAnalysis analysis) : base(canvas, canvasWidth, canvasHeight, context, features, analysis)
    {
    }

    public override async Task RenderAsync()
    {
        // let x be player.Progress
        // let a be analysis.Segments

        // all a[..i] <= x
        // all a[i..] > x
        int i = Bisect.BisectRight(analysis.Segments, player.Progress.TotalSeconds, s => (double)s.Start);

        // therefore:
        // a[i - 1] <= x
        Segment segment = analysis.Segments[i - 1];
        await RenderTimbre(segment);
    }

    private (double X, double Width) MapTimbreValue(float timbre)
    {
        double offsetX = canvasWidth / 2.0d;
        double remapWidth = canvasWidth - offsetX;

        double width = ((double)timbre).Remap(-TimbreMaxValue, TimbreMaxValue, -remapWidth, remapWidth);
        if (width >= 0)
            return (offsetX, width);
        else
            return (offsetX + width, -width);
    }

    private async Task RenderTimbre(Segment segment)
    {
        DrawColor? drawColor = null;

        double segmentHeight = canvasHeight / (segment.Timbre.Count + 1.0d);
        for (int i = 0; i < segment.Timbre.Count; i++)
        {
            float timbre = segment.Timbre[i];

            DrawColor targetColor = timbre >= 0d ? DrawColor.Green : DrawColor.Red;
            if (timbre > TimbreMaxValue)
                targetColor = DrawColor.Gray;

            if (drawColor != targetColor)
            {
                drawColor = targetColor;
                await canvas.FillStyleAsync(ColorToHTML[targetColor]);
            }

            (double x, double width) = MapTimbreValue(timbre);

            double y = i * segmentHeight;
            if (i > 0) // first dimension represents the average loudness of the segment
                y += segmentHeight;

            await canvas.FillRectAsync(x, y, width, segmentHeight);
        }
    }
}
