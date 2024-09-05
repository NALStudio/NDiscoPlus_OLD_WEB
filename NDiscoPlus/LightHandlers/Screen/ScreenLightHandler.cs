
using Blazored.LocalStorage;
using MemoryPack;
using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.LightHandlers.Screen;

internal enum ScreenLightCount
{
    Four = 4,
    Six = 6
}

internal partial class ScreenLightHandlerConfig : LightHandlerConfig
{
    public ScreenLightHandlerConfig(string localStoragePath) : base(localStoragePath)
    {
    }

    public ScreenLightCount LightCount { get; init; } = ScreenLightCount.Four;
}

internal class ScreenLightHandler : LightHandler<ScreenLightHandlerConfig>
{
    public const bool UseHDR = false;

    public ScreenLightHandler(ScreenLightHandlerConfig config) : base(config)
    {
    }

    public override string DisplayName => "Screen";

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
        ColorGamut colorGamut = UseHDR ? ColorGamut.DisplayP3 : ColorGamut.sRGB;

        if (Config.LightCount == ScreenLightCount.Four)
            return new ValueTask<NDPLight[]>(GetLights4(colorGamut));
        else if (Config.LightCount == ScreenLightCount.Six)
            return new ValueTask<NDPLight[]>(GetLights6(colorGamut));
        else
            throw new InvalidLightHandlerConfigException("Invalid light count.");
    }

    public override ValueTask<bool> ValidateConfig(ValidationErrorCollector errors)
    {
        return new ValueTask<bool>(true);
    }
}
