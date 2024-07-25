using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Music;

public class LightInterpreter
{
    private readonly Random random = new();
    private Stopwatch? deltaTimeSW;

    private static IEnumerable<(LightId Light, NDPColor Color)> UpdateBackground(TimeSpan progress, NDPData data)
    {
        int index = -1;
        foreach ((LightId lightId, IList<BackgroundTransition> transitions) in data.Effects.BackgroundTransitions)
        {
            index++;

            int bIndex = Bisect.BisectRight(transitions, progress, t => t.Start);
            // transitions[bIndex].Start > progress
            // transitions[bIndex - 1].Start <= progress
            int currentIndex = bIndex - 1;
            // transitions[currentIndex].Start <= progress

            NDPColor paletteColor = data.EffectPalette[index % data.EffectPalette.Count];
            paletteColor = paletteColor.CopyWith(brightness: data.EffectConfig.BaseBrightness);

            NDPColor color;
            if (currentIndex > -1)
            {
                BackgroundTransition current = transitions[currentIndex];

                NDPColor prevColor;
                if (currentIndex > 0)
                {
                    BackgroundTransition prev = transitions[currentIndex - 1];
                    if (prev.End > current.Start)
                        throw new InvalidOperationException("Cannot run multiple background transitions on the same light simultaneously.");
                    prevColor = prev.Color;
                }
                else
                {
                    prevColor = paletteColor;
                }

                Debug.Assert(current.Start <= progress);

                color = current.Interpolate(progress, prevColor);
            }
            else
            {
                color = paletteColor;
            }

            yield return (lightId, color);
        }
    }

    private static void UpdateEffects(EffectConfig config, TimeSpan progress, IList<Effect> effects, ref Dictionary<LightId, NDPColor> lights)
    {
        // exclusive
        int endIndex = Bisect.BisectRight(effects, progress, t => t.Start);
        // effects[endIndex].Start > progress
        // effects[endIndex - 1].Start <= progress

        // inclusive
        int startIndex = Bisect.BisectLeft(effects, progress, 0, endIndex, t => t.End);
        // effects[startIndex].End >= progress
        // effects[startIndex].End < progress

        // Effects that are later in the list override previous effects
        for (int i = startIndex; i < endIndex; i++)
        {
            Effect effect = effects[i];

            if (!lights.TryGetValue(effect.LightId, out NDPColor oldColor))
            {
                oldColor = effect.GetColor(config.StrobeColor)
                                 .CopyWith(brightness: 0d);
            }

            lights[effect.LightId] = effect.Interpolate(progress, oldColor);
        }
    }

    public IReadOnlyDictionary<LightId, NDPColor> Update(TimeSpan progress, NDPData data)
    {
        Dictionary<LightId, NDPColor> lights = UpdateBackground(progress, data).ToDictionary(key => key.Light, value => value.Color);

        // iterate in reverse so that later channels override the previous channels (strobes override flashes, flashes override default effects, ...)
        for (int i = (data.Effects.Effects.Count - 1); i >= 0; i--)
        {
            IList<Effect> channel = data.Effects.Effects[i];
            UpdateEffects(data.EffectConfig, progress, channel, ref lights);
        }

        double deltaTime;
        if (deltaTimeSW is not null)
        {
            deltaTime = deltaTimeSW.Elapsed.TotalSeconds;
            deltaTimeSW.Restart();
        }
        else
        {
            deltaTimeSW = Stopwatch.StartNew();
            deltaTime = 0d;
        }

        // TODO: Clamp color to light gamut
        // foreach (LightId id in lights.Keys)
        //     lights[id] = lights[id].Clamp()
        return lights.AsReadOnly();
    }
}
