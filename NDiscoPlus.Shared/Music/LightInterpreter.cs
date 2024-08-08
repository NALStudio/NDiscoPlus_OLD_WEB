using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Music;

public readonly record struct LightInterpreterResult
{
    public LightInterpreterResult(ReadOnlyDictionary<LightId, NDPColor> lights, double frameTime)
    {
        Lights = lights;
        FrameTime = frameTime;
    }

    public IDictionary<LightId, NDPColor> Lights { get; }

    public double FrameTime { get; }
    public double FPS => 1d / FrameTime;
}

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
        // find initial reference index (must be done using position as the effects are insorted using this value)
        int initialEndIndex = Bisect.BisectRight(effects, progress, t => t.Position);
        int endIndex = initialEndIndex;
        // effects[endIndex].Position > progress
        // effects[endIndex - 1].Position <= progress

        // find the actual end index value
        while (endIndex < effects.Count && effects[endIndex].Start <= progress)
            endIndex++; // endIndex is exclusive


        // find initial reference index
        int startIndex = Bisect.BisectLeft(effects, progress, 0, initialEndIndex, t => t.Position);
        // effects[startIndex].Position >= progress
        // effects[startIndex - 1].Position < progress

        // find the actual start index value
        // special startIndex < effects.Count verification is needed as startIndex might have bisected to the end of the list
        // we check it as inverse and or it as we still want to look for the earlier values' starts even though startIndex >= effects.Count
        while (startIndex >= 0 && (startIndex >= effects.Count || effects[startIndex].End >= progress))
            startIndex--;
        startIndex++; // increment by one so that startIndex is inclusive

        // Effects that are later in the list override previous effects (sorted using effect.Position)
        for (int i = startIndex; i < endIndex; i++)
        {
            Effect effect = effects[i];

            if (!lights.TryGetValue(effect.LightId, out NDPColor oldColor))
            {
                // if no previous color found, use effect color
                // if effect doesn't have color, use strobe color
                oldColor = effect.GetColor(config.StrobeColor)
                                 .CopyWith(brightness: 0d);
            }

            lights[effect.LightId] = effect.Interpolate(progress, oldColor);
        }
    }

    public LightInterpreterResult Update(TimeSpan progress, NDPData data)
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

        // supply color values for all lights
        foreach (NDPLight l in data.Lights.Values)
        {
            if (lights.TryGetValue(l.Id, out NDPColor color))
            {
                // if lights exists, clamp color to gamut (if gamut is available)
                if (l.ColorGamut is not null)
                    lights[l.Id] = color.Clamp(l.ColorGamut);
            }
            else
            {
                // if light doesn't exist, create a black for it (must be inside its color gamut so we use the color gamut's red XY position.)
                // we supply a default black value so that the consumer of this interpreter doesn't need to assign default colors itself.
                NDPColor defaultBlack;
                if (l.ColorGamut is not null)
                    defaultBlack = l.ColorGamut.Red.ToColor(brightness: 0d);
                else
                    defaultBlack = new NDPColor();

                lights[l.Id] = defaultBlack;
            }
        }

        return new LightInterpreterResult(
            lights: lights.AsReadOnly(),
            frameTime: deltaTime
        );
    }
}
