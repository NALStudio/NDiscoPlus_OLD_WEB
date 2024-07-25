
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
internal class RandomStrobeLightEffect : NDPEffect
{
    public RandomStrobeLightEffect(EffectIntensity intensity) : base(intensity) { }

    public override void Generate(EffectContext ctx, EffectAPI api)
    {
        EffectChannel? channel = api.GetChannel<StrobeEffectChannel>();
        if (channel is null)
            return;
        channel.Clear(ctx.Start, ctx.End);

        NDPColor strobeResetColor = api.Config.StrobeColor.CopyWith(brightness: 0d);
        EffectChannel? effectChannel = api.GetChannel<DefaultEffectChannel>();
        if (effectChannel is not null)
        {
            foreach (NDPLight light in effectChannel.Lights)
                channel.Add(new Effect(light.Id, ctx.Start, ctx.Duration, strobeResetColor));
        }

        int groupCount = GroupedStrobeLightEffect.CalculateGroupCount(ctx);
        int lightsPerFrame = Math.Max(channel.Lights.Count / groupCount, 1);

        HashSet<LightId>? previousFrame = null;
        foreach (NDPInterval tatum in ctx.Tatums)
        {
            HashSet<LightId> currentFrame = new(lightsPerFrame);

            for (int i = 0; i < lightsPerFrame; i++)
            {
                LightId? light = null;
                do
                {
                    light = ctx.Random.Choice(channel.Lights.Values).Id;
                } while (light is null || currentFrame.Contains(light) || (previousFrame?.Contains(light) == true));

                currentFrame.Add(light);
                channel.Add(Effect.CreateStrobe(api.Config, light, tatum.Start, tatum.Duration));
            }

            previousFrame = currentFrame;
        }
    }
}
