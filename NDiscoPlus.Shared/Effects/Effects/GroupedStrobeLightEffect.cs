
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
internal class GroupedStrobeLightEffect : NDPEffect
{
    public enum GroupingType { Horizontal, Vertical, RandomPattern }

    public GroupedStrobeLightEffect(GroupingType grouping, EffectIntensity intensity) : base(intensity)
    {
        Grouping = grouping;
    }

    public GroupingType Grouping { get; }

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

        int groupCount = CalculateGroupCount(ctx);
        List<NDPLight[]> groups = Grouping switch
        {
            GroupingType.Horizontal => channel.Lights.GroupX(groupCount),
            GroupingType.Vertical => channel.Lights.GroupZ(groupCount),
            GroupingType.RandomPattern => ctx.Random.Group(channel.Lights.Values, groupCount).ToList(),
            _ => throw new NotImplementedException()
        };
        Debug.Assert(groups.Count == groupCount);

        for (int i = 0; i < ctx.Tatums.Count; i++)
        {
            NDPInterval tatum = ctx.Tatums[i];
            int groupIndex = i % groupCount;

            foreach (NDPLight light in groups[groupIndex])
            {
                channel.Add(
                    Effect.CreateStrobe(
                        api.Config,
                        light.Id,
                        tatum.Start,
                        tatum.Duration
                    )
                );
            }
        }
    }

    internal static int CalculateGroupCount(EffectContext ctx)
    {
        return ctx.TimeSignature;
    }
}
