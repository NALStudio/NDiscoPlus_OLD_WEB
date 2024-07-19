using HueApi.Entertainment.Models;
using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Music;

public class LightInterpreterConfig
{
    public double BaseBrightness { get; init; } = 0.5d;

    public double MinBrightness { get; init; } = 0d;
    public double MaxBrightness { get; init; } = 1d;
}

public class LightInterpreter
{
    public LightInterpreterConfig Config { get; }

    private readonly Random random = new();
    private Stopwatch? deltaTimeSW;

    public LightInterpreter(LightInterpreterConfig config)
    {
        Config = config;
    }

    IEnumerable<(LightId Light, NDPColor Color)> UpdateBackground(TimeSpan progress, NDPData data)
    {
        BackgroundChannel background = data.Effects.Background;

        int index = -1;
        foreach (NDPLight light in data.Effects.Background.Lights.Values)
        {
            index++;

            IList<BackgroundTransition> transitions = background.GetTransitions(light.Id);
            int bIndex = Bisect.BisectRight(transitions, progress, t => t.Start);
            // transitions[bIndex].Start > progress
            // transitions[bIndex - 1].Start <= progress
            int currentIndex = bIndex - 1;
            // transitions[currentIndex].Start <= progress

            NDPColor paletteColor = data.EffectPalette[index % data.EffectPalette.Count];

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

            color = new(color.X, color.Y, Config.BaseBrightness);
            yield return (light.Id, color);
        }
    }

    void UpdateChannel(TimeSpan progress, EffectChannel channel, ref Dictionary<LightId, NDPColor> lights)
    {
        IList<Effect> effects = channel.Effects;

        // exclusive
        int endIndex = Bisect.BisectRight(effects, progress, t => t.Start);
        // effects[endIndex].Start > progress
        // effects[endIndex - 1].Start <= progress

        // inclusive
        int startIndex = Bisect.BisectLeft(effects, progress, 0, endIndex, t => t.End);
        // effects[startIndex].End >= progress
        // effects[startIndex].End < progress

        // Effects are later in the list override previous effects
        for (int i = startIndex; i < endIndex; i++)
        {
            Effect effect = effects[i];
            // BUG: If background has different lights than effects, this fetch fails
            // in the future we should create the old color on the fly so that its x and y are same as effect color, but brightness is 0
            NDPColor oldColor = lights[effect.LightId];
            NDPColor newColor = effect.Interpolate(progress, oldColor);
            double remappedBrightness = newColor.Brightness.Remap(0d, 1d, Config.MinBrightness, Config.MaxBrightness);
            lights[effect.LightId] = new(newColor.X, newColor.Y, remappedBrightness);
        }
    }

    public IReadOnlyDictionary<LightId, NDPColor> Update(TimeSpan progress, NDPData data)
    {
        Dictionary<LightId, NDPColor> lights = UpdateBackground(progress, data).ToDictionary(key => key.Light, value => value.Color);

        IList<EffectChannel> channels = data.Effects.Channels;

        // UNCOMMENT WHEN BACKGROUND TESTING IS FINISHED
        // iterate in reverse so that later channels override the previous channels (strobes override flashes, flashes override default effects, ...)
        // for (int i = channels.Count - 1; i >= 0; i--)
        //     UpdateChannel(progress, channels[i], ref lights);

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
