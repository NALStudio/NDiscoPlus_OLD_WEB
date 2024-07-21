using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Effects.Effects;

internal sealed class BrightLightEffect : NDPEffect
{
    private readonly bool white;
    private readonly bool slow;

    private BrightLightEffect(bool white, bool slow, EffectIntensity intensity) : base(intensity)
    {
        this.white = white;
        this.slow = slow;
    }

    public static BrightLightEffect Default(EffectIntensity intensity)
        => new(white: false, slow: false, intensity: intensity);

    public static BrightLightEffect Slow(EffectIntensity intensity)
        => new(white: false, slow: true, intensity: intensity);

    public static BrightLightEffect White(EffectIntensity intensity)
        => new(white: true, slow: false, intensity: intensity);

    public override void Generate(EffectContext ctx, EffectAPI api)
    {
        EffectChannel? channel = api.GetChannel<DefaultEffectChannel>();
        if (channel is null)
            return;

        int maxSimultaneousAnimations = (int)(channel.Lights.Count * (2d / 3d));

        double syncDuration = slow ? ctx.SecondsPerBar : ctx.SecondsPerBeat;
        if (double.IsPositiveInfinity(syncDuration))
        {
            // if syncDuration == Infinity since Tempo == 0
            // do not generate any effects when this is the case
            // as there are no beats to really generate on anyways...
            Debug.Assert(ctx.Tempo == 0d);
            return;
        }

        IList<NDPInterval> syncIntervals = slow ? ctx.Bars : ctx.Beats;

        double animationDuration = syncDuration * maxSimultaneousAnimations;

        double fadeInSeconds = 0.2d * animationDuration;
        double fadeOutSeconds = 0.8d * animationDuration;

        // TODO: Use strobe light color
        NDPColor? color = white ? NDPColor.FromLinearRGB(1d, 1d, 1d) : null;

        foreach (NDPInterval interval in syncIntervals)
        {
            // I separated this to its own variable
            // as we may want to offset it backwards at some point
            TimeSpan pos = interval.Start;

            NDPLight[] lights = channel.GetAvailableLights(pos).ToArray();
            NDPLight light;
            if (lights.Length > 0)
                light = ctx.Random.Choice(lights);
            else
                light = channel.GetLight(channel.GetBusyEffects(pos).MinBy(e => e.End).LightId);

            Effect eff = new(
                light.Id,
                interval.Start,
                TimeSpan.Zero
            )
            {
                X = color?.X,
                Y = color?.Y,
                Brightness = 1d,
                FadeIn = TimeSpan.FromSeconds(fadeInSeconds),
                FadeOut = TimeSpan.FromSeconds(fadeOutSeconds)
            };

            channel.Add(eff);
        }
    }
}
