using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Effects.API;

internal class EffectAPI
{
    public EffectConfig Config { get; }

    public IList<EffectChannel> Channels => channelsArr;
    private readonly ImmutableArray<EffectChannel> channelsArr;
    private readonly ImmutableDictionary<Channel, EffectChannel> channelsDic;

    public BackgroundChannel Background { get; }

    public EffectAPI(EffectConfig config, NDiscoPlusArgsLights lights)
    {
        KeyValuePair<Channel, EffectChannel> CreateChannel(Channel type)
        {
            IEnumerable<NDPLight> channelLights = lights.Lights.Where(l => l.Key.HasFlag(type))
                                                               .SelectMany(l => l.Value);

            return new(type, new EffectChannel(channelLights));
        }

        Config = config;

        KeyValuePair<Channel, EffectChannel>[] channels = Enum.GetValues<Channel>()
                                                                  .Select(CreateChannel)
                                                                  .ToArray();
        channelsArr = channels.Select(c => c.Value).ToImmutableArray();
        channelsDic = channels.ToImmutableDictionary();

        Background = new(channelsDic[Channel.Background].Lights);
    }

    public EffectChannel GetChannel(Channel type)
        => channelsDic[type];
    public bool TryGetChannel(Channel type, [MaybeNullWhen(false)] out EffectChannel channel)
        => channelsDic.TryGetValue(type, out channel);
}