using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.Shared.Effects.API.Channels.Effects;

/// <summary>
/// <para>Used for effects that should be drawn under other effects that haven't yet finished.</para>
/// <para>Contains the same lights as background.</para>
/// </summary>
public class BackgroundEffectChannel : EffectChannel
{
    public BackgroundEffectChannel(IEnumerable<NDPLight> lights) : base(lights)
    {
    }
}

/// <summary>
/// Used for normal effects.
/// </summary>
public class DefaultEffectChannel : EffectChannel
{
    public DefaultEffectChannel(IEnumerable<NDPLight> lights) : base(lights)
    {
    }
}

/// <summary>
/// Used for intermittent short colored flashes.
/// </summary>
public class FlashEffectChannel : EffectChannel
{
    public FlashEffectChannel(IEnumerable<NDPLight> lights) : base(lights)
    {
    }
}

/// <summary>
/// Used for strong white "strobe" flashes.
/// </summary>
public class StrobeEffectChannel : EffectChannel
{
    public StrobeEffectChannel(IEnumerable<NDPLight> lights) : base(lights)
    {
    }
}