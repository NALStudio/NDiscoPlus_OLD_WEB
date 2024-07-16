using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using System.Collections.Frozen;

namespace NDiscoPlus.Shared.Effects.API;
public class EffectAPI
{
    public IList<EffectChannel> Channels => channels.Values;
    private readonly FrozenDictionary<Type, EffectChannel> channels;

    public BackgroundChannel Background { get; }

    public EffectAPI(params EffectChannel[] channels)
    {
        Dictionary<Type, EffectChannel> chnls = new();
        foreach (EffectChannel c in channels)
        {
            if (!chnls.TryAdd(c.GetType(), c))
                throw new ArgumentException($"Cannot have multiple instances of {c.GetType().Name}.");
        }

        this.channels = chnls.ToFrozenDictionary();

        Background = new BackgroundChannel();
    }


    public T? GetChannel<T>() where T : EffectChannel
    {
        EffectChannel? c = GetChannel(typeof(T));
        if (c is not null)
            return (T)c;
        else
            return null;
    }

    public EffectChannel? GetChannel(Type type)
    {
        if (channels.TryGetValue(type, out EffectChannel? value))
            return value;
        else
            return null;
    }
}
