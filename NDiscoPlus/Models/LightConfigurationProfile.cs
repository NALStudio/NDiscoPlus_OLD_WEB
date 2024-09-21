using Blazored.LocalStorage;
using NDiscoPlus.Constants;
using NDiscoPlus.LightHandlers;
using NDiscoPlus.LightHandlers.Screen;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Models;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Models;

public sealed class LightConfigurationProfile
{
    private class SerializableProfile
    {
        public string Name { get; }
        public ImmutableArray<LightHandlerConfig> Handlers { get; }
        public ImmutableDictionary<LightId, Channel> LightChannelOverrides { get; }

        [JsonConstructor]
        private SerializableProfile(string name, ImmutableArray<LightHandlerConfig> handlers, ImmutableDictionary<LightId, Channel>? lightChannelOverrides)
        {
            Name = name;
            Handlers = handlers;
            LightChannelOverrides = lightChannelOverrides ?? ImmutableDictionary<LightId, Channel>.Empty;  // Might be null when migrating from an older (dev) version
        }

        public static SerializableProfile Construct(LightConfigurationProfile profile)
        {
            ImmutableArray<LightHandlerConfig> handlers = profile.handlers.Select(h => h.ConfigRef).ToImmutableArray();

            return new SerializableProfile(
                profile.Name,
                handlers: handlers,
                lightChannelOverrides: profile.lightChannelOverrides.ToImmutableDictionary()
            );
        }

        public static LightConfigurationProfile Deconstruct(string localStoragePath, SerializableProfile profile)
        {
            return new LightConfigurationProfile(
                localStoragePath: localStoragePath,
                name: profile.Name,
                handlers: profile.Handlers.Select(h => h.CreateLightHandler()),
                lightChannelOverrides: profile.LightChannelOverrides
            );
        }
    }


    private LightConfigurationProfile(string localStoragePath, string name, IEnumerable<LightHandler> handlers, IDictionary<LightId, Channel> lightChannelOverrides)
    {
        this.localStoragePath = localStoragePath;

        Name = name;

        this.handlers = handlers.ToList();
        this.lightChannelOverrides = lightChannelOverrides.ToDictionary();
    }

    public string UniqueId => localStoragePath;

    public string Name { get; set; } = string.Empty;

    private readonly string localStoragePath;

    private readonly List<LightHandler> handlers;
    private readonly Dictionary<LightId, Channel> lightChannelOverrides;

    public IList<LightHandler> Handlers => handlers.AsReadOnly();
    public IDictionary<LightId, Channel> LightChannelOverrides => lightChannelOverrides.AsReadOnly();

    public static async IAsyncEnumerable<LightConfigurationProfile> LoadAll(ILocalStorageService localStorage, bool _createDefault = true)
    {
        IEnumerable<string> keysEnumerable = await localStorage.GetKeysWithPrefix(LocalStoragePaths.LightConfigurationProfilePrefix);
        string[] keys = keysEnumerable.ToArray();

        if (keys.Length < 1)
        {
            if (_createDefault)
                yield return await GetOrCreateDefaultInstance(localStorage);
            yield break;
        }

        foreach (string key in keys)
        {
            LightConfigurationProfile? profile = await Load(localStorage, key);
            if (profile is not null)
                yield return profile;
            // TODO: Warn if profile could not be deserialized.
        }
    }

    private static async Task<LightConfigurationProfile> GetOrCreateDefaultInstance(ILocalStorageService localStorage)
    {
        // Try to return the first profile if any are available
        await foreach (LightConfigurationProfile loaded in LoadAll(localStorage, _createDefault: false))
            return loaded;

        // Create a new default instance and set it as current.
        LightConfigurationProfile profile = InternalCreateNewWithoutSaving();
        await SaveAsCurrent(localStorage, profile);

        return profile;
    }

    public static async Task<LightConfigurationProfile> LoadCurrent(ILocalStorageService localStorage)
    {
        string? currentKey = await localStorage.GetItemAsStringAsync(LocalStoragePaths.LightConfigurationCurrent);
        if (string.IsNullOrEmpty(currentKey))
            return await GetOrCreateDefaultInstance(localStorage);

        LightConfigurationProfile? profile = await Load(localStorage, currentKey);
        if (profile is null)
            return await GetOrCreateDefaultInstance(localStorage);

        return profile;
    }

    public static async Task SaveAsCurrent(ILocalStorageService localStorage, LightConfigurationProfile profile)
    {
        await Save(localStorage, profile);
        await localStorage.SetItemAsStringAsync(LocalStoragePaths.LightConfigurationCurrent, profile.localStoragePath);
    }

    private static async Task<LightConfigurationProfile?> Load(ILocalStorageService localStorage, string key)
    {
        SerializableProfile? serializable = await localStorage.GetItemAsync<SerializableProfile>(key);
        if (serializable is null)
            return null;
        return SerializableProfile.Deconstruct(key, serializable);
    }

    public static ValueTask Save(ILocalStorageService localStorage, LightConfigurationProfile profile)
    {
        SerializableProfile serializable = SerializableProfile.Construct(profile);
        return localStorage.SetItemAsync(profile.localStoragePath, serializable);
    }

    public static async Task<LightConfigurationProfile> CreateNew(ILocalStorageService localStorage)
    {
        LightConfigurationProfile profile = InternalCreateNewWithoutSaving();
        await Save(localStorage, profile);
        return profile;
    }

    private static LightConfigurationProfile InternalCreateNewWithoutSaving()
    {
        string localStoragePath = LocalStoragePaths.LightConfigurationProfilePrefix.NewKey();

        return new LightConfigurationProfile(
            localStoragePath: localStoragePath,
            name: string.Empty,
            handlers: [new ScreenLightHandler(null)],
            lightChannelOverrides: ImmutableDictionary<LightId, Channel>.Empty
        );
    }

    public bool CanAddHandler(LightHandlerImplementation implementation)
    {
        int existingCount = handlers.Count(h => h.GetType() == implementation.Type);
        return existingCount < implementation.MaxCount;
    }
    public bool CanAddHandler(Type type)
        => CanAddHandler(LightHandler.GetImplementation(type));
    public bool CanAddHandler<T>() where T : LightHandler
        => CanAddHandler(typeof(T));

    public bool CanRemoveHandler(LightHandlerImplementation implementation)
    {
        int existingCount = handlers.Count(h => h.GetType() == implementation.Type);
        return existingCount > implementation.MinCount;
    }
    public bool CanRemoveHandler(Type type)
        => CanRemoveHandler(LightHandler.GetImplementation(type));
    public bool CanRemoveHandler<T>() where T : LightHandler
        => CanRemoveHandler(typeof(T));

    public bool TryAddHandler(Type type)
    {
        if (!CanAddHandler(type))
            return false;

        LightHandlerImplementation impl = LightHandler.GetImplementation(type);
        handlers.Add(impl.Constructor(null));
        return true;
    }
    public bool TryAddHandler<T>() where T : LightHandler
        => TryAddHandler(typeof(T));

    public void AddHandler(Type type)
    {
        bool added = TryAddHandler(type);
        if (!added)
            throw new ArgumentException("Cannot add handler. Maximum handlers reached.");
    }
    public void AddHandler<T>() where T : LightHandler
        => AddHandler(typeof(T));

    public bool TryRemoveHandler(LightHandler handler)
    {
        if (!CanRemoveHandler(handler.GetType()))
            return false;

        handlers.Remove(handler);
        return true;
    }
    public void RemoveHandler(LightHandler handler)
    {
        bool removed = TryRemoveHandler(handler);
        if (!removed)
            throw new ArgumentException("Cannot remove handler. Minimum handlers reached.");
    }
}
