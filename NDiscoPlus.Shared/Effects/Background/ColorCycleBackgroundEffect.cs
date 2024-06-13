using HueApi.ColorConverters;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using SkiaSharp;
using System.Diagnostics;

namespace NDiscoPlus.Shared.Effects.BaseEffects;

internal class BackgroundEffectState : EffectState
{
    internal record class AnimationDataRecord
    {
        public RGBColor TargetColor { get; set; }
        public double Progress { get; set; }

        public AnimationDataRecord(RGBColor target)
        {
            TargetColor = target;
            Progress = 0d;
        }
    }

    // class because struct is copied between functions
    internal record class LightDataRecord
    {
        public RGBColor CurrentColor { get; set; }
        public double AnimationCooldown { get; private set; }
        public AnimationDataRecord? Animation { get; set; }

        public LightDataRecord(RGBColor color, double animationCooldown)
        {
            CurrentColor = color;
            AnimationCooldown = animationCooldown;
            Animation = null;
        }

        public void ResetCooldown(double newCooldown)
        {
            AnimationCooldown = newCooldown;
        }

        public RGBColor UpdateColor(double deltaTime)
        {
            if (Animation is AnimationDataRecord adr)
            {
                return UpdateAnimation(deltaTime, adr);
            }
            else
            {
                AnimationCooldown -= deltaTime;
                if (AnimationCooldown < 0d)
                    AnimationCooldown = 0d;

                return CurrentColor;
            }
        }

        private RGBColor UpdateAnimation(double deltaTime, AnimationDataRecord adr)
        {
            adr.Progress += (deltaTime / ColorCycleBackgroundEffect.AnimationSeconds);
            if (adr.Progress > 1d)
            {
                CurrentColor = adr.TargetColor;
                Animation = null;
                return CurrentColor;
            }
            else
            {
                return ColorHelpers.Gradient(CurrentColor, adr.TargetColor, adr.Progress);
            }
        }
    }

    public LightDataRecord[] LightData { get; set; } = Array.Empty<LightDataRecord>();
}

internal sealed class ColorCycleBackgroundEffect : NDPBackgroundEffect
{
    public const double AnimationSeconds = 10d;

    private static double GetRandomAnimationCooldown(Random random)
        => random.NextDouble().Remap(0d, 1d, 2d, 10d);

    public override BackgroundEffectState CreateState(BackgroundStateContext ctx) => new();

    public override void Update(EffectContext ctx, EffectState effectState)
    {
        BackgroundEffectState state = (BackgroundEffectState)effectState;

        if (ctx.NewTrack)
            Reset(ctx, state);

        Debug.Assert(ctx.Lights.Count == state.LightData.Length);
        for (int i = 0; i < state.LightData.Length; i++)
        {
            var lightData = state.LightData[i];

            ctx.Lights[i].Color = lightData.UpdateColor(ctx.DeltaTime);

            if (lightData.AnimationCooldown <= 0d)
            {
                _ = TryCreateAnimation(ctx, state, lightData);
                lightData.ResetCooldown(GetRandomAnimationCooldown(ctx.Random));
            }
        }
    }

    static void Reset(EffectContext ctx, BackgroundEffectState state)
    {
        state.LightData = new BackgroundEffectState.LightDataRecord[ctx.Lights.Count];

        for (int i = 0; i < state.LightData.Length; i++)
        {
            state.LightData[i] = new BackgroundEffectState.LightDataRecord(
                PickNewRandomColor(ctx.Random, ctx.Palette, state),
                GetRandomAnimationCooldown(ctx.Random)
            );
        }
    }

    static bool TryCreateAnimation(EffectContext ctx, BackgroundEffectState state, BackgroundEffectState.LightDataRecord lightData)
    {
        if (lightData.Animation is not null)
            return false;

        RGBColor color = PickNewRandomColor(ctx.Random, ctx.Palette, state);
        if (lightData.CurrentColor == color)
            return false;

        lightData.Animation = new(color);
        return true;
    }

    static RGBColor PickNewRandomColor(Random random, NDPColorPalette palette, BackgroundEffectState state)
    {
        // Custom function so that we can switch to a more fancy randomizer in the future if needed.
        return palette[random.Next(palette.Count)].ToHueColor();
    }
}
