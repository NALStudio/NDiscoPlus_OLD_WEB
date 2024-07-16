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
}

public class BackgroundChannel : Channel
{
    public IList<BackgroundTransition> Transitions => transitions.AsReadOnly();
    private readonly List<BackgroundTransition> transitions = new();

    public void Add(BackgroundTransition transition)
        => Bisect.InsortRight(transitions, transition, t => t.Start);
}
