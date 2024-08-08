using Excubo.Blazor.Canvas.Contexts;
using NDiscoPlus.Spotify.Models;
using SpotifyAPI.Web;

namespace NDiscoPlus.Components;

public abstract class TrackDebugCanvasRender
{
    protected readonly Context2D canvas;

    protected readonly int canvasWidth;
    protected readonly int canvasHeight;

    protected readonly SpotifyPlayerContext player;
    protected readonly TrackAudioFeatures features;
    protected readonly TrackAudioAnalysis analysis;

    public TrackDebugCanvasRender(Context2D canvas, int canvasWidth, int canvasHeight, SpotifyPlayerContext context, TrackAudioFeatures features, TrackAudioAnalysis analysis)
    {
        this.canvas = canvas;

        this.canvasWidth = canvasWidth;
        this.canvasHeight = canvasHeight;

        this.player = context;
        this.features = features;
        this.analysis = analysis;
    }

    public abstract Task RenderAsync();
}
