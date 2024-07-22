using HueApi.Models;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Effects.API.Channels.Effects;

public readonly struct Effect
{
    [JsonConverter(typeof(JsonLightIdConverter))]
    public LightId LightId { get; }
    public TimeSpan Position { get; }
    public TimeSpan Duration { get; }

    public double? X { get; init; } = null;
    public double? Y { get; init; } = null;
    public double? Brightness { get; init; } = null;

    public TimeSpan FadeIn { get; init; } = TimeSpan.Zero;
    public TimeSpan FadeOut { get; init; } = TimeSpan.Zero;

    [JsonIgnore]
    public TimeSpan Start => Position - FadeIn;
    [JsonIgnore]
    public TimeSpan End => Position + Duration + FadeOut;

    public Effect(LightId light, TimeSpan position, TimeSpan duration)
    {
        LightId = light;
        Position = position;
        Duration = duration;
    }

    public Effect(LightId light, TimeSpan position, TimeSpan duration, NDPColor color) : this(light: light, position: position, duration: duration)
    {
        X = color.X;
        Y = color.Y;
        Brightness = color.Brightness;
    }

    public Effect(LightId light, TimeSpan position, TimeSpan duration, double brightness) : this(light: light, position: position, duration: duration)
    {
        Brightness = brightness;
    }

#pragma warning disable IDE0051 // Remove unused private members
    [JsonConstructor]
    private Effect(LightId lightId, TimeSpan position, TimeSpan duration, double? x, double? y, double? brightness, TimeSpan fadeIn, TimeSpan fadeOut)
    {
        LightId = lightId;
        Position = position;
        Duration = duration;
        X = x;
        Y = y;
        Brightness = brightness;
        FadeIn = fadeIn;
        FadeOut = fadeOut;
    }
#pragma warning restore IDE0051 // Remove unused private members

    public NDPColor GetColor(NDPColor baseColor)
        => new(X ?? baseColor.X, Y ?? baseColor.Y, Brightness ?? baseColor.Brightness);

    public NDPColor Interpolate(TimeSpan progress, NDPColor from)
    {
        NDPColor to = GetColor(from);

        if (progress >= Position && progress < (Position + Duration))
            return to;

        double t;
        if (progress < Position)
            t = (progress - Start) / FadeIn;
        else
            t = 1d - ((progress - (Position + Duration)) / FadeOut);

        return NDPColor.Lerp(from, to, t);
    }
}

public class EffectChannel : Channel
{
    public EffectChannel(IList<NDPLight> lights) : base(lights)
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
        => Bisect.InsortRight(effects, effect, e => e.Start);

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
