using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.Effects.Strobes;
internal abstract class BaseStrobeLightEffect : NDPEffect
{
    protected BaseStrobeLightEffect(EffectIntensity intensity) : base(intensity)
    {
    }

    public override void Generate(EffectContext ctx, EffectAPI api)
    {
        EffectChannel? channel = api.GetChannel<StrobeEffectChannel>();
        if (channel is null)
            return;

        ClearChannelsForStrobes(ctx, api);

        int groupCount = ctx.TimeSignature;
        int frameCount = groupCount * ctx.Beats.Count;

        ImmutableArray<IList<LightId>> groups = Group(ctx, channel, frameCount, groupCount).ToImmutableArray();
        Debug.Assert(groups.Length == frameCount);

        GenerateStrobes(ctx, api.Config, channel, groups, groupCount);
    }

    /// <summary>
    /// <para>Generate an IEnumerable with <paramref name="frameCount"/> amount of light arrays (groups).</para>
    /// <para><paramref name="groupCount"/> can be used as guidance to determine how many different groups should be created.</para>
    /// </summary>
    protected abstract IEnumerable<IList<LightId>> Group(EffectContext ctx, EffectChannel channel, int frameCount, int groupCount);

    private static void GenerateStrobes(EffectContext ctx, EffectConfig config, EffectChannel channel, ImmutableArray<IList<LightId>> groups, int groupsPerBeat)
    {
        int beatIndex;
        for (beatIndex = 0; beatIndex < ctx.Beats.Count; beatIndex++)
        {
            NDPInterval beat = ctx.Beats[beatIndex];

            for (int i = 0; i < groupsPerBeat; i++)
            {
                TimeSpan duration = beat.Duration / groupsPerBeat;
                TimeSpan start = beat.Start + (i * duration);

                foreach (LightId light in groups[(beatIndex * groupsPerBeat) + i])
                {
                    channel.Add(
                        Effect.CreateStrobe(
                            config,
                            light,
                            start,
                            duration
                        )
                    );
                }
            }
        }

        Debug.Assert((beatIndex * groupsPerBeat) == groups.Length);
    }

    private static void ClearChannelsForStrobes(EffectContext ctx, EffectAPI api)
    {
        // we sync using beats currently, but this might change in the future
        NDPInterval lastSyncObject = ctx.Beats[ctx.Beats.Count - 1];
        TimeSpan strobeEnd = lastSyncObject.End;
        // Debug.Assert(strobeEnd >= ctx.End); This assert seemed to cause some crashes

        TimeSpan clearStart = ctx.Start;
        TimeSpan clearLength = strobeEnd - clearStart;

        NDPColor strobeResetColor = api.Config.StrobeColor.CopyWith(brightness: 0d);
        foreach (EffectChannel channel in api.Channels)
        {
            foreach (NDPLight light in channel.Lights)
                channel.Add(new Effect(light.Id, clearStart, clearLength, strobeResetColor));
        }
    }
}

