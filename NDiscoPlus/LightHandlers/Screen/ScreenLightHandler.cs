using Blazored.LocalStorage;
using MemoryPack;
using Microsoft.AspNetCore.Components;
using NDiscoPlus.Components.LightHandlerConfigEditor;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using NDiscoPlus.Shared.Music;
using System.Diagnostics;

namespace NDiscoPlus.LightHandlers.Screen;

public enum ScreenLightCount
{
    Four = 4,
    Six = 6
}

public class ScreenLightHandlerConfig : LightHandlerConfig
{
    public ScreenLightCount LightCount { get; set; } = ScreenLightCount.Six;
    public bool UseHDR { get; set; } = false;

    public override LightHandler CreateLightHandler()
        => new ScreenLightHandler(this);
    public override Type GetEditorType() => typeof(ScreenLightHandlerConfigEditor);
}

internal readonly record struct ScreenLight(NDPLight Light, NDPColor? Color)
{
    public ScreenLight ReplaceColor(NDPColor color)
        => new(Light, color);
}

internal class ScreenLightHandler : LightHandler
{
    private record class LightsContainer(NDPLight[] Lights)
    {
        public NDPColor[]? Colors { get; set; } = null;
    }

    private LightsContainer? lights;
    public IReadOnlyList<NDPColor>? Colors => lights?.Colors;

    public ScreenLightHandler(LightHandlerConfig? config) : base(config)
    {
    }

    public override string DisplayName => "Screen";

    public override int MinCount => 1;
    public override int MaxCount => 1;

    public override LightHandlerConfig CreateConfig()
        => new ScreenLightHandlerConfig();

    private static NDPLight[] GetLights4(ColorGamut colorGamut)
    {
        return new NDPLight[]
        {
            new(new ScreenLightId(0), new LightPosition(-1, 1, 1), colorGamut: colorGamut),
            new(new ScreenLightId(1), new LightPosition(1, 1, 1), colorGamut: colorGamut),
            new(new ScreenLightId(2), new LightPosition(-1, -1, -1), colorGamut: colorGamut),
            new(new ScreenLightId(3), new LightPosition(1, -1, -1), colorGamut: colorGamut)
        };
    }

    private static NDPLight[] GetLights6(ColorGamut colorGamut)
    {
        return new NDPLight[]
        {
            new(new ScreenLightId(0), new LightPosition(-1, 1, 1), colorGamut: colorGamut),
            new(new ScreenLightId(1), new LightPosition(0, 1, 1), colorGamut: colorGamut),
            new(new ScreenLightId(2), new LightPosition(1, 1, 1), colorGamut: colorGamut),
            new(new ScreenLightId(3), new LightPosition(-1, -1, -1), colorGamut: colorGamut),
            new(new ScreenLightId(4), new LightPosition(0, -1, -1), colorGamut: colorGamut),
            new(new ScreenLightId(5), new LightPosition(1, -1, -1), colorGamut: colorGamut)
        };
    }

    private NDPLight[] GetLightsInternal()
    {
        var config = Config<ScreenLightHandlerConfig>();

        ColorGamut colorGamut = config.UseHDR ? ColorGamut.DisplayP3 : ColorGamut.sRGB;

        if (config.LightCount == ScreenLightCount.Four)
            return GetLights4(colorGamut);
        else if (config.LightCount == ScreenLightCount.Six)
            return GetLights6(colorGamut);
        else
            throw new InvalidLightHandlerConfigException("Invalid light count.");
    }

    public override ValueTask<NDPLight[]> GetLights()
        => new(GetLightsInternal());

    public override ValueTask<bool> ValidateConfig(ErrorMessageCollector errors)
    {
        ScreenLightHandlerConfig config = Config<ScreenLightHandlerConfig>();

        bool valid = true;

        if (!Enum.GetValues<ScreenLightCount>().Contains(config.LightCount))
        {
            errors.Add($"Invalid Screen Light Count: {config.LightCount}");
            valid = false;
        }

        return new ValueTask<bool>(valid);
    }

    public override ValueTask<bool> Start(ErrorMessageCollector errors, out NDPLight[] lights)
    {
        if (this.lights is null)
        {
            NDPLight[] l = GetLightsInternal();
            this.lights = new LightsContainer(l);
        }

        lights = this.lights.Lights;

        return new ValueTask<bool>(true);
    }

    public override ValueTask Update(LightColorCollection lightColors)
    {
        if (lights is null)
            throw new InvalidOperationException("Screen Light Handler not started.");
        lights.Colors ??= new NDPColor[lights.Lights.Length];

        foreach ((ScreenLightId light, NDPColor color) in lightColors.OfType<ScreenLightId>()) // get lights of correct type
            lights.Colors[light.Index] = color;

        return new();
    }

    public override ValueTask Stop()
    {
        lights = null;
        return new();
    }
}
