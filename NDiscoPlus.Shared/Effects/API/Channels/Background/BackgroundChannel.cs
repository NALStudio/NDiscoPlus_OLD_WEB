﻿using MemoryPack;
using NDiscoPlus.Shared.Effects.API.Channels.Background.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Effects.API.Channels.Background;

public class BackgroundChannel : BaseChannel
{
    public BackgroundChannel(NDPLightCollection lights) : base(lights)
    {
    }

    private readonly Dictionary<LightId, List<BackgroundTransition>> transitions = new();

    public void Add(BackgroundTransition transition)
    {
        if (!transitions.TryGetValue(transition.LightId, out List<BackgroundTransition>? trans))
        {
            trans = new();
            transitions.Add(transition.LightId, trans);
        }

        Bisect.InsortRight(trans, transition, t => t.Start);
    }

    /// <summary>
    /// <para>Get the currently running transition for the given light.</para>
    /// <para>If not transitions are running, returns the next transition scheduled.</para>
    /// <para>If there are no transitions whatsoever or the light doesn't exist, returns null.</para>
    /// </summary>
    public BackgroundTransition? GetAt(LightId light, TimeSpan position)
    {
        if (!transitions.TryGetValue(light, out List<BackgroundTransition>? trans))
            return null;

        // trans[..index].Start <= position
        // trans[index..].Start > position
        int index = Bisect.BisectRight(trans, position, t => t.Start);
        if (index > 0 && trans[index - 1].End > position)
            return trans[index - 1];
        else if (trans.Count > 0)
            return trans[index];
        else
            return null;
    }

    internal ImmutableDictionary<LightId, ImmutableArray<BackgroundTransition>> Freeze()
        => transitions.ToImmutableDictionary(key => key.Key, value => value.Value.ToImmutableArray());
}
