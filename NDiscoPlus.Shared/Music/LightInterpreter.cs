using HueApi.ColorConverters;
using HueApi.ColorConverters.Original.Extensions;
using HueApi.Models;
using NDiscoPlus.Shared.Effects.BaseEffects;
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

    public double BaseBrightness { get; init; } = 0.3d;
    public double MaxBrightness { get; init; } = 1d;
}

public class LightInterpreter
{
    private readonly record struct EffectData(NDPBaseEffect Effect, EffectState State)
    {
        public static EffectData CreateForEffect(NDPBaseEffect effect)
        {
            EffectState state = effect.CreateState();
            return new(effect, state);
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

    private EffectData backgroundEffect = EffectData.CreateForEffect(new BackgroundEffect());

    void Reset()
    {
        barIndex = -1;
        beatIndex = -1;
        tatumIndex = -1;
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

    private static (int BeatIndex, bool BeatChanged) UpdateBeats(TimeSpan progress, IList<NDPInterval> timings, int previousIndex)
    {
        int newIndex = FindBeatIndex(progress, timings, previousIndex);
        return (newIndex, newIndex != previousIndex);
    }

    public IReadOnlyList<NDPLight> Update(TimeSpan progress, NDPData data)
    {
        bool isNewTrack = false;
        if (trackId != data.Track.Id)
        {
            trackId = data.Track.Id;
            isNewTrack = true;
            Reset();
        }

        // data update
        (barIndex, bool newBar) = UpdateBeats(progress, data.Timings.Bars, barIndex);
        (beatIndex, bool newBeat) = UpdateBeats(progress, data.Timings.Beats, beatIndex);
        (tatumIndex, bool newTatum) = UpdateBeats(progress, data.Timings.Tatums, tatumIndex);

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
            lights: Lights,
            palette: data.EffectPalette,

            random: random,

            progress: progress,
            deltaTime: deltaTime,

            newTrack: isNewTrack,

            barIndex: barIndex,
            newBar: newBar,
            beatIndex: beatIndex,
            newBeat: newBeat,
            tatumIndex: tatumIndex,
            newTatum: newTatum
        );

        // light update
        backgroundEffect.Effect.Update(ctx, backgroundEffect.State);

        return Lights;
    }
}
