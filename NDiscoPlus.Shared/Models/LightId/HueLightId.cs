namespace NDiscoPlus.Shared.Models;
public class HueLightId : LightId
{
    public byte ChannelId { get; }

    public HueLightId(byte channelId)
    {
        ChannelId = channelId;
    }

    public override int GetHashCode()
        => HashCode.Combine(GetType(), ChannelId);
}
