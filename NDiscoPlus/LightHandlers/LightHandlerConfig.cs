using NDiscoPlus.LightHandlers.Screen;
using System.Text.Json.Serialization;

namespace NDiscoPlus.LightHandlers;

[JsonDerivedType(typeof(ScreenLightHandlerConfig), typeDiscriminator: "screen")]
internal abstract class LightHandlerConfig
{
    /// <summary>
    /// This method is called to create a light handler from the given configuration.
    /// </summary>
    public abstract LightHandler CreateLightHandler();
}