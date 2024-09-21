using Blazored.LocalStorage;
using NDiscoPlus.Constants;
using NDiscoPlus.LightHandlers;
using NDiscoPlus.LightHandlers.Screen;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Models;

public sealed class LightConfigurationProfile
{
    private class SerializableProfile
    {
        public string Name { get; }
        public ImmutableArray<LightHandlerConfig> Handlers { get; }

        [JsonConstructor]
        private SerializableProfile(string name, ImmutableArray<LightHandlerConfig> handlers)
        {
            Name = name;
            Handlers = handlers;
        }

        public static SerializableProfile Construct(LightConfigurationProfile profile)
        {
            ImmutableArray<LightHandlerConfig> handlers = profile.handlers.Select(h => h.ConfigRef).ToImmutableArray();

            return new SerializableProfile(
                profile.Name,
                handlers: handlers
            );
        }
    }

    private readonly string localStoragePath;
    private readonly List<LightHandler> handlers;

    [JsonConstructor]
    private LightConfigurationProfile(string localStoragePath, string name, IEnumerable<LightHandler> handlers)
    {
        this.localStoragePath = localStoragePath;

        Name = name;
        this.handlers = handlers.ToList();
    }

    public string UniqueId => localStoragePath;

    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public IReadOnlyList<LightHandler> Handlers => handlers.AsReadOnly();

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

        return new LightConfigurationProfile(
            localStoragePath: key,
            name: serializable.Name,
            handlers: serializable.Handlers.Select(h => h.CreateLightHandler())
        );
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

        return new(
            localStoragePath: localStoragePath,
            name: string.Empty,
            handlers: [new ScreenLightHandler(null)]
        );
    }

    public bool CanAddHandler<T>(T handler) where T : LightHandler
    {
        int existingCount = handlers.Count(h => h is T);
        return existingCount < handler.MaxCount;
    }

    public bool CanRemoveHandler<T>(T handler) where T : LightHandler
    {
        int existingCount = handlers.Count(h => h is T);
        return existingCount > handler.MinCount;
    }

    public bool TryAddHandler<T>(T handler) where T : LightHandler
    {
        if (!CanAddHandler(handler))
            return false;

        handlers.Add(handler);
        return true;
    }

    public void AddHandler<T>(T handler) where T : LightHandler
    {
        bool added = TryAddHandler(handler);
        if (!added)
            throw new ArgumentException("Cannot add handler. Maximum handlers reached.");
    }

    public bool TryRemoveHandler<T>(T handler) where T : LightHandler
    {
        if (!CanRemoveHandler(handler))
            return false;

        handlers.Remove(handler);
        return true;
    }

    public void RemoveHandler<T>(T handler) where T : LightHandler
    {
        bool removed = TryRemoveHandler(handler);
        if (!removed)
            throw new ArgumentException("Cannot remove handler. Minimum handlers reached.");
    }
}
