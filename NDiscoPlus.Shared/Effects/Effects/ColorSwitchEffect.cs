using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.Effects;
internal class ColorSwitchEffect : NDPEffect
{
    public ColorSwitchEffect(EffectIntensity intensity) : base(intensity)
    {
    }

    public override void Generate(EffectContext ctx, EffectAPI api)
    {
        EffectChannel? channel = api.GetChannel<BackgroundEffectChannel>();
        if (channel is null)
            return;

        double beatsPerAnimationDouble = (double)(2 * ctx.TimeSignature) / channel.Lights.Count;

        int beatsPerAnimation;
        int lightsPerAnimation;
        if (beatsPerAnimationDouble >= 1)
        {
            beatsPerAnimation = (int)beatsPerAnimationDouble;
            lightsPerAnimation = 1;
        }
        else
        {
            // we expect:
            // 0 < beatsPerAnimationDouble < 1
            Debug.Assert(beatsPerAnimationDouble > 0);

            beatsPerAnimation = 1;
            lightsPerAnimation = (int)(1 / beatsPerAnimationDouble);
        }

        NDPColor[] paletteColors = ctx.Palette.ToArray();
        Dictionary<LightId, NDPColor> colors = channel.Lights.Values.ToDictionary(key => key.Id, _ => ctx.Random.Choice(paletteColors));

        HashSet<LightId> changedLights = new();
        for (int i = 0; i < ctx.Beats.Count; i += beatsPerAnimation)
        {
            int endIndex = i + beatsPerAnimation;
            if (endIndex > ctx.Beats.Count)
                endIndex = ctx.Beats.Count;

            NDPInterval[] beats = ctx.Beats.ToArray()[i..endIndex];

            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (NDPInterval beat in beats)
                totalDuration += beat.Duration;

            changedLights.Clear();

            for (int j = 0; j < lightsPerAnimation; j++)
            {
                NDPLight? light = null;
                do
                {
                    light = ctx.Random.Choice(channel.Lights.Values);
                }
                while (light is null || changedLights.Contains(light.Value.Id));
                LightId lightId = light.Value.Id;

                NDPColor? color = null;
                do
                {
                    color = ctx.Random.Choice(ctx.Palette.Colors);
                }
                while (!color.HasValue || color.Value == colors[lightId]);

                changedLights.Add(lightId);
                colors[lightId] = color.Value;
            }

            foreach ((LightId id, NDPColor col) in colors)
            {
                channel.Add(
                    new Effect(
                        id,
                        beats[0].Start,
                        totalDuration
                    )
                    {
                        X = col.X,
                        Y = col.Y,
                        Brightness = api.Config.BaseBrightness
                    }
                );
            }
        }
    }
}
