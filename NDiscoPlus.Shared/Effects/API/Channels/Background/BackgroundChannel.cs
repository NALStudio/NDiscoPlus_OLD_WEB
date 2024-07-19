using HueApi.Models;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;

namespace NDiscoPlus.Shared.Effects.API.Channels.Background;

public readonly struct BackgroundTransition
{
    public LightId LightId { get; }

    public TimeSpan Start { get; }
    public TimeSpan Duration { get; }
    public TimeSpan End => Start + Duration;

    public NDPColor Color { get; }

    public BackgroundTransition(LightId light, TimeSpan start, TimeSpan duration, NDPColor color)
    {
        LightId = light;
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

public class BackgroundChannel : Channel
{
    public BackgroundChannel(IList<NDPLight> lights) : base(lights)
    {
    }

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

    public IList<BackgroundTransition> GetTransitions(LightId light)
        => transitions[light].AsReadOnly();
}
