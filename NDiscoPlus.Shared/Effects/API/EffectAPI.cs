using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace NDiscoPlus.Shared.Effects.API;

internal class EffectAPI
{
    public IList<EffectChannel> Channels => channels.Values;
    private readonly FrozenDictionary<Type, EffectChannel> channels;

    public BackgroundChannel Background { get; }

    public EffectAPI(EffectChannel[] channels, BackgroundChannel background)
    {
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
}
