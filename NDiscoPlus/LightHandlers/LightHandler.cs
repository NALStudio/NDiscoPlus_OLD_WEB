﻿using MemoryPack;
using NDiscoPlus.LightHandlers.Screen;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using NDiscoPlus.Shared.Music;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace NDiscoPlus.LightHandlers;

internal abstract class LightHandler : IAsyncDisposable
{
    private readonly LightHandlerConfig config;

    protected T Config<T>() where T : LightHandlerConfig => (T)config;
    public LightHandlerConfig Config() => config;

    protected LightHandler(LightHandlerConfig? config)
    {
        this.config = config ?? CreateConfig();
    }

    public abstract string DisplayName { get; }

    /// <summary>
    /// Inclusive. (example: MinCount 1, LightHandler cannot be removed if it is the only handler remaining)
    /// </summary>
    public virtual int MinCount => 0;
    /// <summary>
    /// Inclusive. (example: MinCount 1, LightHandler cannot be added if a handler already exists)
    /// </summary>
    public virtual int MaxCount => 3;

    /// <summary>
    /// This method is called by the constructor to create a default config instance for the object if none was passed to the constructor.
    /// </summary>
    public abstract LightHandlerConfig CreateConfig();

    public abstract ValueTask<bool> ValidateConfig(ErrorMessageCollector errors);

    public abstract ValueTask<NDPLight[]> GetLights();

    /// <summary>
    /// <para>Start the handler if possible. </para>
    /// <para>If handler is already running, should result in a no-op.</para>
    /// </summary>
    public abstract ValueTask<bool> Start(ErrorMessageCollector errors, out NDPLight[] lights);
    public abstract ValueTask Update(LightInterpreterResult result);

    /// <summary>
    /// If handler isn't running, should result in a no-op.
    /// </summary>
    public abstract ValueTask Stop();

    public ValueTask DisposeAsync() => Stop();
}