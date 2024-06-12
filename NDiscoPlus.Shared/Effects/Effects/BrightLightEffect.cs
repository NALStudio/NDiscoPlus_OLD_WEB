using HueApi.ColorConverters;
using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Effects.Effect;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.Effects;

internal class BrightLightState : EffectState
{
    public enum AnimationState
    {
        Forward,
        Backward,
        Idle
    }

    public class Animation
    {
        public const double ForwardSpeed = 0.5;
        public const double BackwardSpeed = 2.0;

        public int LightIndex { get; }

        public double Brightness { get; private set; } = 0d;
        public AnimationState State { get; private set; } = AnimationState.Idle;

        public double SpeedMultiplier { get; }

        public bool IsIdle => State == AnimationState.Idle;

        public Animation(int lightIndex)
        {
            LightIndex = lightIndex;
            SpeedMultiplier = 1d;
        }

        public Animation(int lightIndex, bool slow)
        {
            LightIndex = lightIndex;
            SpeedMultiplier = slow ? 0.5d : 1d;
        }

        public void Update(double deltaTime)
        {
            if (State == AnimationState.Forward)
            {
                Brightness += ForwardSpeed * deltaTime * SpeedMultiplier;
                if (Brightness >= 1d)
                {
                    Brightness = 1d;
                    State = AnimationState.Backward;
                }
            }
            else if (State == AnimationState.Backward)
            {
                Brightness -= BackwardSpeed * deltaTime * SpeedMultiplier;
                if (Brightness <= 0d)
                {
                    Brightness = 0d;
                    State = AnimationState.Idle;
                }
            }
        }

        public void RunForward()
        {
            State = AnimationState.Forward;
        }
    }

    public ImmutableArray<Animation> Animations { get; }

    public BrightLightState(int lightCount, bool slow)
    {
        Animations = Enumerable.Range(0, lightCount)
            .Select(i => new Animation(i, slow))
            .ToImmutableArray();
    }
}

internal sealed class BrightLightEffect : NDPEffect
{
    private bool white;
    private EffectSync sync;

    private BrightLightEffect(bool white, EffectSync sync, EffectIntensity intensity) : base(intensity)
    {
        this.white = white;
        this.sync = sync;
    }

    public static BrightLightEffect Default()
        => new(white: false, sync: EffectSync.Beat, intensity: EffectIntensity.Medium);

    public static BrightLightEffect Slow()
        => new(white: false, sync: EffectSync.Bar, intensity: EffectIntensity.Low);

    public static BrightLightEffect White()
        => new(white: true, sync: EffectSync.Beat, intensity: EffectIntensity.High);

    public override EffectState CreateState(StateContext ctx) => new BrightLightState(ctx.LightCount, sync == EffectSync.Bar);

    public override void Update(EffectContext ctx, EffectState effectState)
    {
        BrightLightState state = (BrightLightState)effectState;

        if (ctx.GetSync(sync))
            NewAnimation(ctx, state);

        for (int i = 0; i < state.Animations.Length; i++)
        {
            var anim = state.Animations[i];
            var light = ctx.Lights[i];

            anim.Update(ctx.DeltaTime);

            light.Brightness = anim.Brightness;
            if (white)
            {
                // light.Color is set by background effect, we override it here.
                light.Color = ColorHelpers.Gradient(light.Color, new RGBColor(1d, 1d, 1d), anim.Brightness);
            }
        }
    }

    private static void NewAnimation(EffectContext ctx, BrightLightState state)
    {
        BrightLightState.Animation[] idle = state.Animations.Where(a => a.IsIdle).ToArray();
        if (idle.Length > 0)
            idle[ctx.Random.Next(idle.Length)].RunForward();
        else
            state.Animations.MinBy(a => a.Brightness)?.RunForward();
    }
}
