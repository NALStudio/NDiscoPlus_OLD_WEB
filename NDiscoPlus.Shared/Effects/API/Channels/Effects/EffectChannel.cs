using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Effects.API.Channels.Effects;

public class EffectChannel : Channel
{
    public EffectChannel(IEnumerable<NDPLight> lights) : base(lights)
    {
    }

    [JsonIgnore]
    public IList<Effect> Effects => effects.AsReadOnly();

    [JsonInclude]
    private readonly List<Effect> effects = new();

#pragma warning disable IDE0051 // Remove unused private members
    [JsonConstructor]
    private EffectChannel(NDPLightCollection lights, List<Effect> effects) : base(lights.Values.ToArray())
    {
        this.effects = effects;
    }
#pragma warning restore IDE0051 // Remove unused private members

    public void Add(Effect effect)
        => Bisect.InsortRight(effects, effect, e => e.Position);

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
    /// Clear all effects that clip with the given range.
    /// If you want to only remove values that are contained within the range (start and end inside)
    /// use <see cref="Clear"/> instead.
    /// </para>
    /// <para>Start is inclusive, end is exclusive.</para>
    /// </summary>
    public void Purge(TimeSpan start, TimeSpan end)
        => effects.RemoveAll(e => e.End >= start && e.Start < end);

    public IEnumerable<Effect> GetBusyEffects(TimeSpan position)
        => effects.Where(e => e.End >= position && e.Start < position);

    public IEnumerable<NDPLight> GetBusyLights(TimeSpan position)
        => GetBusyLightsInternal(position).Select(id => Lights[id]);

    private HashSet<LightId> GetBusyLightsInternal(TimeSpan position)
    {
        HashSet<LightId> ids = new();
        foreach (Effect eff in GetBusyEffects(position))
            _ = ids.Add(eff.LightId);
        return ids;
    }

    public IEnumerable<NDPLight> GetAvailableLights(TimeSpan position)
    {
        HashSet<LightId> reserved = GetBusyLightsInternal(position);
        foreach (NDPLight light in Lights.Values)
        {
            if (!reserved.Contains(light.Id))
                yield return light;
        }
    }
}
