using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.Shared.Effects.API.Channels.Effects;

public readonly struct Effect
{
    public LightId LightId { get; }
    public TimeSpan Position { get; }
    public TimeSpan Duration { get; }

    public NDPColor? Color { get; init; } = null;
    public double? Brightness { get; init; } = null;

    public TimeSpan FadeIn { get; init; } = TimeSpan.Zero;
    public TimeSpan FadeOut { get; init; } = TimeSpan.Zero;

    public TimeSpan Start => Position - FadeIn;
    public TimeSpan End => Position + Duration + FadeOut;

    public Effect(LightId light, TimeSpan position, TimeSpan duration)
    {
        LightId = light;
        Position = position;
        Duration = duration;
    }
}

public class EffectChannel : Channel
{
    public IList<Effect> Effects => effects.AsReadOnly();
    private readonly List<Effect> effects = new();

    public void Add(Effect effect)
        => effects.Add(effect);

    /// <summary>
    /// <para>
    /// Clear all effects that are contained within the given range.
    /// If you want to also remove values that clip with the range (start outside and end inside, or vice versa)
    /// use <see cref="Purge"/> instead.
    /// </para>
    /// <para>Start is inclusive, end is exclusive.</para>
    /// </summary>
    public void Clear(TimeSpan start, TimeSpan end)
        => effects.RemoveAll(e => e.Start >= start && e.End < end);

    /// <summary>
    /// <para>
    /// Clear all effects that are clip with the given range.
    /// If you want to only remove values that are contained within the range (start and end inside)
    /// use <see cref="Clear"/> instead.
    /// </para>
    /// <para>Start is inclusive, end is exclusive.</para>
    /// </summary>
    public void Purge(TimeSpan start, TimeSpan end)
        => effects.RemoveAll(e => e.End >= start || e.Start < end);
}
