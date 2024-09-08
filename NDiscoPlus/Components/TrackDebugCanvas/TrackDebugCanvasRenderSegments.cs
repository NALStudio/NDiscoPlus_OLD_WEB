using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using MudBlazor;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Spotify.Models;
using SpotifyAPI.Web;
using System.Diagnostics;

namespace NDiscoPlus.Components;

public class TrackDebugCanvasRenderSegments : TrackDebugCanvasRender
{
    private readonly record struct SegmentRender(Segment Segment, double X, double Width)
    {
        public bool GetVisible(double canvasWidth)
        {
            double Left = X;
            double Right = X + Width;
            return (Right >= 0d) && (Left <= canvasWidth);

        }
    }

    double windowSizeSeconds = 5d;

    const double minLoudness = -60;
    const double maxLoudness = 0;

    public TrackDebugCanvasRenderSegments(Context2D canvas, int canvasWidth, int canvasHeight, SpotifyPlayerContext context, TrackAudioFeatures features, TrackAudioAnalysis analysis) : base(canvas, canvasWidth, canvasHeight, context, features, analysis)
    {
    }

    public override async Task RenderAsync()
    {
        Segment current = await RenderSegments();
        await RenderPlayer(current);
    }

    double GetScrollingX(double start)
    {
        double halfWindowSize = windowSizeSeconds / 2d;
        double progress = player.Progress.TotalSeconds;

        double windowLeft = progress - halfWindowSize;
        double windowRight = progress + halfWindowSize;

        return start.Remap(windowLeft, windowRight, 0d, canvasWidth);
    }

    double GetWidth(double duration)
    {
        double frac = duration / windowSizeSeconds;
        return frac * canvasWidth;
    }

    static int SegmentBinarySearch(List<Segment> segments, double progress)
    {
        int first = 0;
        int last = segments.Count - 1;
        int mid = 0;
        do
        {
            mid = first + (last - first) / 2;
            if (progress > segments[mid].Start)
                first = mid + 1;
            else
                last = mid - 1;
            if (segments[mid].Start == progress)
                return mid;
        } while (first <= last);
        return mid;
    }

    async Task<Segment> RenderSegments()
    {
        int currentSegmentIndex = SegmentBinarySearch(analysis.Segments, player.Progress.TotalSeconds);

        await canvas.LineWidthAsync(1);

        SegmentRender[] renders = VisibleRenders(currentSegmentIndex, analysis.Segments).ToArray();

        await canvas.FillStyleAsync("hsl(0, 100%, 50%)");
        await canvas.StrokeStyleAsync("hsl(0, 100%, 35%)");
        foreach (SegmentRender r in renders)
            await RenderSegmentMax(r);

        await canvas.FillStyleAsync("hsl(60, 100%, 50%)");
        await canvas.StrokeStyleAsync("hsl(60, 100%, 35%)");
        foreach (SegmentRender r in renders)
            await RenderSegmentRise(r);

        await canvas.FillStyleAsync("hsl(120, 100%, 50%)");
        await canvas.StrokeStyleAsync("hsl(120, 100%, 35%)");
        foreach (SegmentRender r in renders)
            await RenderSegment(r);

        return analysis.Segments[currentSegmentIndex];
    }

    IEnumerable<SegmentRender> VisibleRenders(int currentIndex, List<Segment> segments)
    {
        SegmentRender CreateRender(int index)
        {
            Segment segment = segments[index];
            return new SegmentRender(segment, GetScrollingX(segment.Start), GetWidth(segment.Duration));
        }

        for (int i = currentIndex; i >= 0; i--)
        {
            SegmentRender rend = CreateRender(i);
            if (!rend.GetVisible(canvasWidth))
                break;
            yield return rend;
        }

        for (int i = currentIndex + 1; i < analysis.Segments.Count; i++)
        {
            SegmentRender rend = CreateRender(i);
            if (!rend.GetVisible(canvasWidth))
                break;
            yield return rend;
        }
    }

    async Task RenderSegment(SegmentRender segment)
    {
        double segmentHeight = DoubleHelpers.Remap(segment.Segment.LoudnessStart, minLoudness, maxLoudness, 0, canvasHeight);

        await canvas.FillRectAsync(segment.X, canvasHeight - segmentHeight, segment.Width, segmentHeight);
        // await canvas.StrokeRectAsync(segment.X, canvasHeight - segmentHeight, segment.Width, segmentHeight);
    }

    async Task RenderSegmentMax(SegmentRender render)
    {
        Segment segment = render.Segment;

        double maxHeight = DoubleHelpers.Remap(segment.LoudnessMax, minLoudness, maxLoudness, 0, canvasHeight);
        double maxX = GetScrollingX(segment.Start + segment.LoudnessMaxTime);
        double maxWidth = GetWidth(segment.Duration - segment.LoudnessMaxTime);

        await canvas.FillRectAsync(maxX, canvasHeight - maxHeight, maxWidth, maxHeight);
        // await canvas.StrokeRectAsync(maxX, canvasHeight - maxHeight, maxWidth, maxHeight);
    }

    async Task RenderSegmentRise(SegmentRender render)
    {
        Segment segment = render.Segment;

        double maxHeight = DoubleHelpers.Remap(segment.LoudnessMax, minLoudness, maxLoudness, 0, canvasHeight);

        double width = GetWidth(segment.LoudnessMaxTime);

        await canvas.FillRectAsync(render.X, canvasHeight - maxHeight, width, maxHeight);
    }

    async Task RenderPlayer(Segment currentSegment)
    {
        double pos = canvasWidth / 2d;

        await canvas.StrokeStyleAsync("#000000");
        await canvas.LineWidthAsync(2);

        await canvas.BeginPathAsync();
        await canvas.MoveToAsync(pos, 0d);
        await canvas.LineToAsync(pos, canvasHeight);
        await canvas.StrokeAsync();

        string[] lines = [
            $"{currentSegment.LoudnessStart} dB for: {currentSegment.Duration} sec",
            $"{currentSegment.LoudnessMax} dB (max) at: {currentSegment.LoudnessMaxTime} sec",
            $"{currentSegment.Confidence * 100:.00} % confidence"
        ];

        double textY = 0d;
        foreach (string line in lines)
        {
            TextMetrics measured = await canvas.MeasureTextAsync(line);
            textY += measured.FontBoundingBoxAscent + measured.FontBoundingBoxDescent;
            await canvas.FillTextAsync(line, pos + 1, textY);
        }
    }
}
