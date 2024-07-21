using HueApi.Models;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Collections;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Effects.API.Channels.Background;

public readonly struct BackgroundTransition
{
    [JsonConverter(typeof(JsonLightIdConverter))]
    public LightId LightId { get; }

    public TimeSpan Start { get; }
    public TimeSpan Duration { get; }

    [JsonIgnore]
    public TimeSpan End => Start + Duration;

    public NDPColor Color { get; }

    [JsonConstructor]
    public BackgroundTransition(LightId lightId, TimeSpan start, TimeSpan duration, NDPColor color)
    {
        LightId = lightId;
        Start = start;
        Duration = duration;
        Color = color;
    }

    public NDPColor Interpolate(TimeSpan progress, NDPColor from)
    {
        double t = (progress - Start) / Duration;
        return NDPColor.Lerp(from, Color, t);
    }
}

public class BackgroundChannel : Channel, IEnumerable<KeyValuePair<LightId, IList<BackgroundTransition>>>
{
    public BackgroundChannel(IList<NDPLight> lights) : base(lights)
    {
    }

#pragma warning disable IDE0051 // Remove unused private members
    [JsonConstructor]
    private BackgroundChannel(NDPLightCollection lights, Dictionary<LightId, List<BackgroundTransition>> transitions) : base(lights.Values.ToArray())
    {
        this.transitions = transitions;
    }
#pragma warning restore IDE0051 // Remove unused private members

    [JsonInclude]
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

    public IEnumerator<KeyValuePair<LightId, IList<BackgroundTransition>>> GetEnumerator()
    {
        foreach (KeyValuePair<LightId, List<BackgroundTransition>> trans in transitions)
            yield return new(trans.Key, trans.Value.AsReadOnly());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
