using HueApi.ColorConverters;
using HueApi.ColorConverters.Original.Extensions;
using HueApi.Models;
using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Effects.Effects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using SkiaSharp;
using SpotifyAPI.Web;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Music;

public class LightInterpreterConfig
{
    public double BackgroundRPM { get; init; } = 5d;

    public double BaseBrightness { get; init; } = 0.5d;
    public double MaxBrightness { get; init; } = 1d;
}

public class LightInterpreter
{
    private record EffectData(NDPEffect Effect, EffectState State)
    {
        public static EffectData? CreateForEffect(LightInterpreter parent, EffectRecord effect, EffectState? previousState)
        {
            if (effect.Effect is null)
                return null;

            StateContext ctx = new(
                lightCount: parent.Lights.Count,
                sectionData: new SectionData(effect.Section),
                previousState: previousState
            );

            EffectState state = effect.Effect.CreateState(ctx);
            return new(effect.Effect, state);
        }
    }

    public LightInterpreterConfig Config { get; }
    public NDPLightCollection Lights { get; }

    private string? trackId;

    private int barIndex;
    private int beatIndex;
    private int tatumIndex;


    private Random random = new();
    private Stopwatch? deltaTimeSW;

    private NDPBackgroundEffect? backgroundEffect;
    private EffectState? backgroundEffectState;

    private int effectIndex;
    private EffectData? effect;

    void Reset()
    {
        barIndex = -1;
        beatIndex = -1;
        tatumIndex = -1;

        effectIndex = -1;
    }

    public LightInterpreter(LightInterpreterConfig config, params NDPLight[] lights)
    {
        Config = config;
        Lights = NDPLightCollection.Create(lights);
    }

    private static int FindBeatIndex(TimeSpan progress, IList<NDPInterval> timings, int previousIndex)
    {
        if (previousIndex >= 0 && previousIndex < timings.Count)
        {
            // if previous index in range, check next beat in the future first where it most likely resorts (if we haven't seeked)

            if (timings[previousIndex].Contains(progress))
                return previousIndex;

            for (int i = (previousIndex + 1); i < timings.Count; i++)
            {
                if (timings[i].Contains(progress))
                    return i;
            }

            for (int i = (previousIndex - 1); i >= 0; i--)
            {
                if (timings[i].Contains(progress))
                    return i;
            }

            return -1;
        }
        else
        {
            // if previous index isn't in range, the new track is either shorter than the previous track or there isn't a previous track yet
            // in this case just do a linear search.

            for (int i = 0; i < timings.Count; i++)
            {
                if (timings[i].Contains(progress))
                    return i;
            }

            return -1;
        }
    }

    private static (int NewIndex, bool IndexChanged) UpdateIndex(TimeSpan progress, IList<NDPInterval> timings, int previousIndex)
    {
        int newIndex = FindBeatIndex(progress, timings, previousIndex);
        return (newIndex, newIndex != previousIndex);
    }

    public IReadOnlyList<NDPLight> Update(TimeSpan progress, NDPData data)
    {

        backgroundEffect ??= new ColorCycleBackgroundEffect();
        backgroundEffectState ??= backgroundEffect.CreateState(new BackgroundStateContext(lightCount: Lights.Count));

        bool isNewTrack = false;
        if (trackId != data.Track.Id)
        {
            trackId = data.Track.Id;
            isNewTrack = true;
            Reset();
        }

        // data update
        (barIndex, bool newBar) = UpdateIndex(progress, data.Timings.Bars, barIndex);
        (beatIndex, bool newBeat) = UpdateIndex(progress, data.Timings.Beats, beatIndex);
        (tatumIndex, bool newTatum) = UpdateIndex(progress, data.Timings.Tatums, tatumIndex);

        (effectIndex, bool newEffect) = UpdateIndex(progress, data.Effects.Select(e => e.Interval).ToArray(), effectIndex);
        if (newEffect)
        {
            if (effectIndex < 0)
            {
                effect = null;
            }
            else
            {
                EffectState? oldState = effect?.State;
                effect = EffectData.CreateForEffect(
                    this,
                    data.Effects[effectIndex],
                    oldState
                );
            }
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


        EffectContext ctx = new(
            config: Config,

            lights: Lights,
            palette: data.EffectPalette,

            random: random,

            progress: progress,
            deltaTime: deltaTime,

            newTrack: isNewTrack,

            barTiming: TimingContext.Construct(progress, barIndex, newBar, data.Timings.Bars),
            beatTiming: TimingContext.Construct(progress, beatIndex, newBar, data.Timings.Beats),
            tatumTiming: TimingContext.Construct(progress, tatumIndex, newBar, data.Timings.Tatums)
        );

        // light update
        backgroundEffect.Update(ctx, backgroundEffectState);
        effect?.Effect.Update(ctx, effect.State);

        return Lights;
    }
}
