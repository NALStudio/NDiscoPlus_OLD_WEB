using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Effects.StrobeAnalyzers;
using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Effects.Strobes;

internal class SegmentBurstStrobes : NDPStrobe
{
    const int MinSegmentCount = 4;

    public override void Generate(Context ctx, EffectAPI api)
    {
        foreach (ImmutableArray<NDPInterval> burst in ctx.Analysis.Segments.Bursts)
            GenerateForBurst(api, burst);
    }

    private static void GenerateForBurst(EffectAPI api, ImmutableArray<NDPInterval> burst)
    {
        int groupCount = burst.Length;

        // reduce the groups to a more manageable count
        if (groupCount % 5 == 0)
            groupCount = 5;
        else if (groupCount % 4 == 0)
            groupCount = 4;
        else if (groupCount % 3 == 0)
            groupCount = 3;
        else
            groupCount = 2;

        StrobeEffectChannel channel = api.GetChannel<StrobeEffectChannel>();
        List<NDPLight[]> lightGroups = channel.Lights.GroupX(groupCount);

        // Console.WriteLine(groupCount);
        // Console.WriteLine(groupCount);
        // Console.WriteLine(groupCount);
        // foreach (NDPLight[] group in lightGroups)
        // {
        //     Console.WriteLine("[");
        //     foreach (NDPLight light in group)
        //         Console.WriteLine($"    {light.Position.X}");
        //     Console.WriteLine("]");
        // }

        Debug.Assert(lightGroups.Count == groupCount);

        for (int i = 0; i < burst.Length; i++)
        {
            NDPInterval b = burst[i];
            foreach (NDPLight light in lightGroups[i % groupCount])
                channel.Add(Effect.CreateStrobe(api.Config, light.Id, b));
        }
    }
}
