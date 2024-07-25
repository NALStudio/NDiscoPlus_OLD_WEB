using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Effects.Effects.Strobes;
internal class RandomStrobeLightEffect : BaseStrobeLightEffect
{
    public RandomStrobeLightEffect(EffectIntensity intensity) : base(intensity)
    {
    }

    protected override IEnumerable<IList<LightId>> Group(EffectContext ctx, EffectChannel channel, int frameCount, int groupCount)
    {
        int lightsPerFrame = Math.Max(channel.Lights.Count / groupCount, 1);

        List<HashSet<LightId>> output = new(capacity: frameCount);

        for (int i = 0; i < frameCount; i++)
        {
            HashSet<LightId> currentFrame = new(capacity: lightsPerFrame);
            HashSet<LightId>? lastFrame = output.Count > 0 ? output[^1] : null;

            for (int j = 0; j < lightsPerFrame; j++)
            {
                LightId light;
                do
                {
                    light = ctx.Random.Choice(channel.Lights.Values).Id;
                } while (currentFrame.Contains(light) || (lastFrame?.Contains(light) == true));

                bool wasAdded = currentFrame.Add(light);
                Debug.Assert(wasAdded);
            }

            output.Add(currentFrame);
        }

        foreach (HashSet<LightId> o in output)
            yield return o.ToImmutableArray();
    }
}
