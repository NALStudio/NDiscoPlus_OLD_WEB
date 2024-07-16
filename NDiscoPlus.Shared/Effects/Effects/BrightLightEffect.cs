using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;

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

    public override void Generate(EffectContextX ctx, EffectAPI api)
    {
        EffectChannel? channel = api.GetChannel<DefaultEffectChannel>();
        if (channel is null)
            return;

        int maxSimultaneousAnimations = (int)(channel.Lights.Count * (2d / 3d));

        double syncDuration = slow ? ctx.SecondsPerBar : ctx.SecondsPerBeat;
        IList<NDPInterval> syncIntervals = slow ? ctx.Bars : ctx.Beats;

        double animationDuration = syncDuration * maxSimultaneousAnimations;

        double fadeInSeconds = 0.2d * animationDuration;
        double fadeOutSeconds = 0.8d * animationDuration;

        NDPColor? color = white ? NDPColor.FromLinearRGB(1d, 1d, 1d) : null;

        foreach (NDPInterval interval in syncIntervals)
        {
            // I separated this to its own variable
            // as we may want to offset it backwards at some point
            TimeSpan pos = interval.Start;

            NDPLight[] lights = channel.GetAvailableLights(pos).ToArray();
            NDPLight l = ctx.Random.Choice(lights);

            Effect eff = new(
                l.Id,
                interval.Start,
                TimeSpan.FromSeconds(animationDuration)
            )
            {
                Color = color,
                Brightness = 1d,
                FadeIn = TimeSpan.FromSeconds(fadeInSeconds),
                FadeOut = TimeSpan.FromSeconds(fadeOutSeconds)
            };

            channel.Add(eff);
        }
    }
}
