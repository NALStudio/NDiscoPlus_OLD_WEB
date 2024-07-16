using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Models;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Music;

public class LightInterpreterConfig
{
    public double BaseBrightness { get; init; } = 0.5d;
    public double MaxBrightness { get; init; } = 1d;
}

public class LightInterpreter
{
    public LightInterpreterConfig Config { get; }
    public NDPLightCollection Lights { get; }

    private readonly Random random = new();
    private Stopwatch? deltaTimeSW;

    public LightInterpreter(LightInterpreterConfig config, params NDPLight[] lights)
    {
        Config = config;
        Lights = NDPLightCollection.Create(lights);
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
