using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Music;

public class LightInterpreterConfig
{
    public double BaseBrightness { get; init; } = 0.1d;

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
            yield return (lightId, color);
        }
    }

    void UpdateEffects(TimeSpan progress, IList<Effect> effects, ref Dictionary<LightId, NDPColor> lights)
    {
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

            if (!lights.TryGetValue(effect.LightId, out NDPColor oldColor))
                oldColor = effect.GetColor(NDPColor.FromLinearRGB(1d, 1d, 1d));

            NDPColor newColor = effect.Interpolate(progress, oldColor);
            double remappedBrightness = newColor.Brightness.Remap(0d, 1d, Config.MinBrightness, Config.MaxBrightness);
            lights[effect.LightId] = new(newColor.X, newColor.Y, remappedBrightness);
        }
    }

    public IReadOnlyDictionary<LightId, NDPColor> Update(TimeSpan progress, NDPData data)
    {
        Dictionary<LightId, NDPColor> lights = UpdateBackground(progress, data).ToDictionary(key => key.Light, value => value.Color);

        // iterate in reverse so that later channels override the previous channels (strobes override flashes, flashes override default effects, ...)
        UpdateEffects(progress, data.Effects.Effects, ref lights);

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
