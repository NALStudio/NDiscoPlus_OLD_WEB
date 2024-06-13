using HueApi.ColorConverters;
using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpotifyAPI.Web.PlayerSetRepeatRequest;

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
        public static readonly (double forw, double back) AnimationRatio = (1, 4);

        public int LightIndex { get; }

        public double Progress { get; private set; }
        public bool IsFinished => (Progress >= Duration);

        public double Duration { get; }
        public double ForwDuration => Duration * (AnimationRatio.forw / (AnimationRatio.forw + AnimationRatio.back));
        public double BackDuration => Duration * (AnimationRatio.back / (AnimationRatio.forw + AnimationRatio.back));

        public RGBColor? TargetColor { get; }


        public Animation(int lightIndex, double duration, RGBColor? targetColor)
        {
            LightIndex = lightIndex;
            Duration = duration;

            Progress = duration;

            TargetColor = targetColor;
        }

        public double Update(double deltaTime)
        {
            if (Progress >= Duration)
                return GetBrightness();

            Progress += deltaTime;
            return GetBrightness();
        }

        private double GetBrightness()
        {
            double p = Progress.Clamp(0, Duration);
            if (p < ForwDuration)
            {
                return p / ForwDuration;
            }
            else
            {
                double pp = p - ForwDuration;
                double back = pp / BackDuration;
                return 1d - back;
            }
        }

        public void Restart()
        {
            Progress = 0d;
        }
    }

    public ImmutableArray<Animation> Animations { get; }
    public double ForwardSpeed { get; }
    public double BackwardSpeed { get; }

    public BrightLightState? PreviousState { get; }

    public BrightLightState(StateContext ctx, bool white, bool slow)
    {
        int maxSimultaneousAnimations = (int)(ctx.LightCount * (2d / 3d));

        double syncDuration = slow ? ctx.SectionData.SecondsPerBar : ctx.SectionData.SecondsPerBeat;

        double animationDuration = syncDuration * maxSimultaneousAnimations;

        Animations = Enumerable.Range(0, ctx.LightCount)
            .Select(i => new Animation(i, animationDuration, white ? new RGBColor(1d, 1d, 1d) : null))
            .ToImmutableArray();

        PreviousState = (ctx.PreviousState as BrightLightState);
    }
}

internal sealed class BrightLightEffect : NDPEffect
{
    private bool white;
    private bool slow;

    private EffectSync Sync => slow ? EffectSync.Bar : EffectSync.Beat;

    private BrightLightEffect(bool white, bool slow, EffectIntensity intensity) : base(intensity)
    {
        this.white = white;
        this.slow = slow;
    }

    public static BrightLightEffect Default(EffectIntensity intensity)
        => new(white: false, slow: false, intensity: intensity);

    public static BrightLightEffect Slow(EffectIntensity intensity)
        => new(white: false, slow: true, intensity: intensity);

    public static BrightLightEffect White(EffectIntensity intensity)
        => new(white: true, slow: false, intensity: intensity);

    public override EffectState CreateState(StateContext ctx) => new BrightLightState(ctx, white: white, slow: slow);

    public override void Update(EffectContext ctx, EffectState effectState)
    {
        BrightLightState state = (BrightLightState)effectState;

        if (ctx.GetSync(Sync))
            NewAnimation(ctx, state);

        UpdateAnimations(ctx, state.Animations);

        if (state.PreviousState is not null)
            UpdateAnimations(ctx, state.PreviousState.Animations, ignoreIfFinished: true);
    }

    public static void UpdateAnimations(EffectContext ctx, IList<BrightLightState.Animation> animations, bool ignoreIfFinished = false)
    {
        for (int i = 0; i < animations.Count; i++)
        {
            var anim = animations[i];
            var light = ctx.Lights[i];

            double brightness = anim.Update(ctx.DeltaTime);

            if (anim.IsFinished && ignoreIfFinished)
                continue;

            light.Brightness = DoubleHelpers.Lerp(ctx.Config.BaseBrightness, ctx.Config.MaxBrightness, brightness);
            if (anim.TargetColor is RGBColor target)
            {
                // light.Color is set by background effect, we override it here.
                light.Color = ColorHelpers.Gradient(light.Color, target, brightness);
            }
        }
    }

    private static void NewAnimation(EffectContext ctx, BrightLightState state)
    {
        BrightLightState.Animation[] idle = state.Animations.Where(a => a.IsFinished).ToArray();
        if (state.PreviousState is not null)
        {
            idle = idle.Where(a =>
            {
                BrightLightState.Animation oldAnim = state.PreviousState.Animations[a.LightIndex];
                Debug.Assert(oldAnim.LightIndex == a.LightIndex);
                return oldAnim.IsFinished;
            }).ToArray();
        }

        if (idle.Length > 0)
            idle[ctx.Random.Next(idle.Length)].Restart();
        else // fallback if tempo and bars/beats don't match or the previous animation has swallowed all idle slots
            state.Animations.MaxBy(a => a.Progress)?.Restart();
    }
}
