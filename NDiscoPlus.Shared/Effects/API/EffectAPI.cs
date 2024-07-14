using NDiscoPlus.Shared.Effects.API.Channels;
using System.Collections.Frozen;

namespace NDiscoPlus.Shared.Effects.API;
public class EffectAPI
{
    public EffectAPI(params EffectChannel[] channels)
    {
        Dictionary<Type, EffectChannel> chnls = new();
        foreach (EffectChannel c in channels)
        {
            if (!chnls.TryAdd(c.GetType(), c))
                throw new ArgumentException($"Cannot have multiple instances of {c.GetType().Name}.");
        }

        this.channels = FrozenDictionary.ToFrozenDictionary(chnls);
    }

    readonly FrozenDictionary<Type, EffectChannel> channels;

    public T? GetChannel<T>() where T : EffectChannel
    {
        if (channels.TryGetValue(typeof(T), out EffectChannel? value))
            return (T)value;
        else
            return null;
    }
}
