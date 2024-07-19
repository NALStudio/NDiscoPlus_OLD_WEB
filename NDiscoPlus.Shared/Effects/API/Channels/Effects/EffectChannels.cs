using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.Shared.Effects.API.Channels.Effects;

/// <summary>
/// Used for normal effects.
/// </summary>
public class DefaultEffectChannel : EffectChannel
{
    public DefaultEffectChannel(IList<NDPLight> lights) : base(lights)
    {
    }
}

/// <summary>
/// Used for intermittent short colored flashes.
/// </summary>
public class FlashEffectChannel : EffectChannel
{
    public FlashEffectChannel(IList<NDPLight> lights) : base(lights)
    {
    }
}

/// <summary>
/// Used for strong white "strobe" flashes.
/// </summary>
public class StrobeEffectChannel : EffectChannel
{
    public StrobeEffectChannel(IList<NDPLight> lights) : base(lights)
    {
    }
}