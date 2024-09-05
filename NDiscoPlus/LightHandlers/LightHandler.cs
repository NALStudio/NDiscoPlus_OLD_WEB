using MemoryPack;
using NDiscoPlus.LightHandlers.Screen;
using NDiscoPlus.Shared.Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace NDiscoPlus.LightHandlers;

public struct ValidationErrorCollector
{
    private List<string>? errors;

    public ValidationErrorCollector()
    {
        errors = new();
    }

    public readonly void Add(string msg)
    {
        if (errors is null)
            throw new InvalidOperationException("Cannot add new errors after Collect() is called.");

        errors.Add(msg);
    }

    public IList<string> Collect()
    {
        if (errors is null)
            throw new InvalidOperationException("Cannot call Collect() twice.");

        ReadOnlyCollection<string> err = errors.AsReadOnly();
        errors = null;
        return err;
    }
}

internal class InvalidLightHandlerConfigException : Exception
{
    public InvalidLightHandlerConfigException() : base()
    {
    }

    public InvalidLightHandlerConfigException(string? message) : base(message)
    {
    }

    public InvalidLightHandlerConfigException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

[JsonDerivedType(typeof(ScreenLightHandler), typeDiscriminator: "screen")]
internal abstract class LightHandlerConfig
{
    public string LocalStoragePath { get; }

    protected LightHandlerConfig(string localStoragePath)
    {
        LocalStoragePath = localStoragePath;
    }
}

internal abstract class LightHandler<T> where T : LightHandlerConfig
{
    protected T Config { get; }

    protected LightHandler(T config)
    {
        Config = config;
    }

    public abstract string DisplayName { get; }

    public abstract ValueTask<bool> ValidateConfig(ValidationErrorCollector errors);

    public abstract ValueTask<NDPLight[]> GetLights();
}
