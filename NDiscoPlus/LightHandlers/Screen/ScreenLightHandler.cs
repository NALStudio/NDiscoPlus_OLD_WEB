using Blazored.LocalStorage;
using MemoryPack;
using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.LightHandlers.Screen;

internal enum ScreenLightCount
{
    Four = 4,
    Six = 6
}

internal class ScreenLightHandlerConfig : LightHandlerConfig
{
    public ScreenLightCount LightCount { get; set; } = ScreenLightCount.Six;

    public override LightHandler CreateLightHandler()
        => new ScreenLightHandler(this);
}

internal class ScreenLightHandler : LightHandler
{
    public const bool UseHDR = false;

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

    public override ValueTask<NDPLight[]> GetLights()
    {
        var config = Config<ScreenLightHandlerConfig>();

        ColorGamut colorGamut = UseHDR ? ColorGamut.DisplayP3 : ColorGamut.sRGB;

        if (config.LightCount == ScreenLightCount.Four)
            return new ValueTask<NDPLight[]>(GetLights4(colorGamut));
        else if (config.LightCount == ScreenLightCount.Six)
            return new ValueTask<NDPLight[]>(GetLights6(colorGamut));
        else
            throw new InvalidLightHandlerConfigException("Invalid light count.");
    }

    public override ValueTask<bool> ValidateConfig(ValidationErrorCollector errors)
    {
        return new ValueTask<bool>(true);
    }
}
