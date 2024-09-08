﻿using MemoryPack;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
public partial class HueLightId : LightId
{
    public Guid EntertainmentConfigurationId { get; }
    public byte ChannelId { get; }

    public override string HumanReadableString => $"Hue Light (id: {ChannelId})";

    public HueLightId(Guid entertainmentConfigurationId, byte channelId)
    {
        EntertainmentConfigurationId = entertainmentConfigurationId;
        ChannelId = channelId;
    }

    public override int GetHashCode()
        => HashCode.Combine(GetType(), ChannelId);

    public override bool Equals(object? obj)
        => obj is HueLightId hli && ChannelId == hli.ChannelId;
}
