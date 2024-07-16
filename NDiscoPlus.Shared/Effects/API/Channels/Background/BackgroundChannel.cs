using NDiscoPlus.Shared.Models.NDPColor;

namespace NDiscoPlus.Shared.Effects.API.Channels.Background;

internal readonly struct BackgroundChannelPoint
{
    public NDPColor Color { get; }

    public TimeSpan Position { get; }
    public TimeSpan Duration { get; }
}

public class BackgroundChannel : Channel
{

}
