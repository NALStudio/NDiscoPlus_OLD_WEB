namespace NDiscoPlus.Shared.Models;
public class HueLightId : LightId
{
    public byte ChannelId { get; }

    public HueLightId(byte channelId)
    {
        ChannelId = channelId;
    }
}
