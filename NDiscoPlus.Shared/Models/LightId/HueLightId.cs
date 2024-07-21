namespace NDiscoPlus.Shared.Models;
public class HueLightId : LightId
{
    public byte ChannelId { get; }

    public override string HumanReadableString => $"Hue Light (id: {ChannelId})";

    public HueLightId(byte channelId)
    {
        ChannelId = channelId;
    }

    public override int GetHashCode()
        => HashCode.Combine(GetType(), ChannelId);

    public override bool Equals(object? obj)
        => obj is HueLightId hli && ChannelId == hli.ChannelId;
}
