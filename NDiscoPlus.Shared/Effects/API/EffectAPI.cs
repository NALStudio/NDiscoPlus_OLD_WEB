using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Effects.API;

internal class EffectAPI
{
    public EffectConfig Config { get; }

    public IList<EffectChannel> Channels => channels.Values;
    private readonly FrozenDictionary<Type, EffectChannel> channels;

    public BackgroundChannel Background { get; }

    public EffectAPI(EffectConfig config, EffectChannel[] channels, BackgroundChannel background)
    {
        Config = config;

        Dictionary<Type, EffectChannel> chnls = new();
        foreach (EffectChannel c in channels)
        {
            if (!chnls.TryAdd(c.GetType(), c))
                throw new ArgumentException($"Cannot have multiple instances of {c.GetType().Name}.");
        }

        this.channels = chnls.ToFrozenDictionary();

        Background = background;
    }


    public T GetChannel<T>() where T : EffectChannel
        => (T)GetChannel(typeof(T));

    public EffectChannel GetChannel(Type type)
        => channels[type];

    public bool TryGetChannel<T>([MaybeNullWhen(false)] out T channel) where T : EffectChannel
    {
        if (TryGetChannel(typeof(T), out EffectChannel? chnl))
        {
            channel = (T)chnl;
            return true;
        }
        else
        {
            channel = null;
            return false;
        }

    }
    public bool TryGetChannel(Type type, [MaybeNullWhen(false)] out EffectChannel channel)
        => channels.TryGetValue(type, out channel);

    public ExportedEffectsCollection Export()
    {
        FrozenDictionary<LightId, ImmutableArray<BackgroundTransition>> backgroundTransitions = Background.ToFrozenDictionary(key => key.Key, value => value.Value.ToImmutableArray());

        return new ExportedEffectsCollection(
            effects: Channels.Select(c => c.Effects),
            backgroundTransitions: Background.Select(x => new KeyValuePair<LightId, IList<BackgroundTransition>>(x.Key, x.Value))
        );
    }
}