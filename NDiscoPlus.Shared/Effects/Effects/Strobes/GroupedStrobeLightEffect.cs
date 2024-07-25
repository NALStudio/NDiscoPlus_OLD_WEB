using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Effects.Effects.Strobes;
internal class GroupedStrobeLightEffect : BaseStrobeLightEffect
{
    public enum GroupingType { Horizontal, Vertical, RandomPattern }

    public GroupedStrobeLightEffect(GroupingType grouping, EffectIntensity intensity) : base(intensity)
    {
        Grouping = grouping;
    }

    public GroupingType Grouping { get; }

    protected override IEnumerable<IList<LightId>> Group(EffectContext ctx, EffectChannel channel, int frameCount, int groupCount)
    {
        List<NDPLight[]> groups = Grouping switch
        {
            GroupingType.Horizontal => channel.Lights.GroupX(groupCount),
            GroupingType.Vertical => channel.Lights.GroupZ(groupCount),
            GroupingType.RandomPattern => ctx.Random.Group(channel.Lights.Values, groupCount).ToList(),
            _ => throw new NotImplementedException()
        };
        Debug.Assert(groups.Count == groupCount);

        ImmutableArray<LightId>[] idGroups = groups.Select(g => g.Select(l => l.Id).ToImmutableArray()).ToArray();
        for (int i = 0; i < frameCount; i++)
            yield return idGroups[i % groupCount];
    }
}
