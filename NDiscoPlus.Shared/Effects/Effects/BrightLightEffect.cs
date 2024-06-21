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
        public int LightIndex { get; }

        public double ProgressSeconds { get; private set; }
        public bool IsFinished => ProgressSeconds >= DurationSeconds;

        public double FadeInSeconds { get; }
        public double FadeOutSeconds { get; }
        public double DurationSeconds => FadeInSeconds + FadeOutSeconds;

        public RGBColor? TargetColor { get; }


        public Animation(int lightIndex, BrightLightState parent)
        {
            LightIndex = lightIndex;

            ProgressSeconds = 0d;

            FadeInSeconds = parent.FadeInSeconds;
            FadeOutSeconds = parent.FadeOutSeconds;

            TargetColor = parent.White ? new RGBColor(1d, 1d, 1d) : null;
        }

        public double Update(double deltaTime)
        {
            if (IsFinished)
                throw new InvalidOperationException("Animation was updated after finish.");
            ProgressSeconds += deltaTime;
            return GetBrightness();
        }

        private double GetBrightness()
        {
            double p = ProgressSeconds.Clamp(0, DurationSeconds);
            if (p < FadeInSeconds)
            {
                return p / FadeInSeconds;
            }
            else
            {
                double pp = p - FadeInSeconds;
                double back = pp / FadeOutSeconds;
                return 1d - back;
            }
        }
    }

    public double FadeInSeconds { get; }
    public double FadeOutSeconds { get; }

    public List<Animation> Animations { get; }
    public bool White { get; }

    public int LatestIndex { get; set; }

    public BrightLightState(StateContext ctx, bool white, bool slow)
    {
        int maxSimultaneousAnimations = (int)(ctx.LightCount * (2d / 3d));

        double syncDuration = slow ? ctx.SectionData.SecondsPerBar : ctx.SectionData.SecondsPerBeat;

        double animationDuration = syncDuration * maxSimultaneousAnimations;

        FadeInSeconds = 0.2d * animationDuration;
        FadeOutSeconds = 0.8d * animationDuration;

        Animations = new();
        White = white;

        if (ctx.PreviousState is BrightLightState previous)
            Animations.AddRange(previous.Animations);
    }
}

internal sealed class BrightLightEffect : NDPEffect
{
    private bool white;
    private bool slow;

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

        TimingContext timing = slow ? ctx.BarTiming : ctx.BeatTiming;

        if (timing.NextIndex != state.LatestIndex
            && timing.UntilNext is TimeSpan un
            && un.TotalSeconds < (state.FadeInSeconds / 2d))
        {
            state.LatestIndex = timing.NextIndex;
            NewAnimation(ctx, state);
        }

        UpdateAnimations(ctx, state.Animations);
    }

    public static void UpdateAnimations(EffectContext ctx, List<BrightLightState.Animation> animations, bool ignoreIfFinished = false)
    {
        foreach (var anim in animations)
        {
            NDPLight light = ctx.Lights[anim.LightIndex];

            double brightness = anim.Update(ctx.DeltaTime);

            light.Brightness = DoubleHelpers.Lerp(ctx.Config.BaseBrightness, ctx.Config.MaxBrightness, brightness);
            if (anim.TargetColor is RGBColor target)
            {
                // light.Color is set by background effect, we override it here.
                light.Color = ColorHelpers.Gradient(light.Color, target, brightness);
            }
        }

        animations.RemoveAll(a => a.IsFinished);
    }

    private static void NewAnimation(EffectContext ctx, BrightLightState state)
    {
        if (state.Animations.Count < ctx.Lights.Count)
        {
            HashSet<int> reservedLights = state.Animations.Select(l => l.LightIndex).ToHashSet();
            int[] freeLights = Enumerable.Range(0, ctx.Lights.Count).Where(index => !reservedLights.Contains(index)).ToArray();
            int lightIndex = freeLights[ctx.Random.Next(freeLights.Length)];
            // TODO: Adjust fade out by the duration until the next bar/beat
            // Fade in and fade out logic probably needs to be separated (different lists?)
            state.Animations.Add(new BrightLightState.Animation(lightIndex, state));
        }
        else
        {
            Debug.Assert(state.Animations.Count > 0);
            // when there are no animation slots left
            // (tempo and bars/beats don't match or previous animation state has swallowed all idle slots)
            int? maxIndex = null;
            for (int i = 0; i < state.Animations.Count; i++)
            {
                if (maxIndex.HasValue)
                {
                    if (state.Animations[i].ProgressSeconds > state.Animations[maxIndex.Value].ProgressSeconds)
                        maxIndex = i;
                }
                else
                {
                    maxIndex = i;
                }
            }

            if (maxIndex.HasValue)
            {
                state.Animations[maxIndex.Value] =
                    new BrightLightState.Animation(state.Animations[maxIndex.Value].LightIndex, state);
            }
        }
    }
}
