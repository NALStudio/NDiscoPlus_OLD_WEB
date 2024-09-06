using Blazored.LocalStorage;
using NDiscoPlus.Constants;
using NDiscoPlus.LightHandlers;
using NDiscoPlus.LightHandlers.Screen;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Models;

internal sealed class LightConfigurationProfile
{
    private readonly string localStoragePath;
    private readonly List<LightHandler> handlers;

    [JsonConstructor]
    private LightConfigurationProfile(string localStoragePath, List<LightHandler> handlers)
    {
        this.localStoragePath = localStoragePath;
        this.handlers = handlers;
    }

    public IList<LightHandler> Handlers => handlers.AsReadOnly();

    public static async IAsyncEnumerable<LightConfigurationProfile> LoadAll(ILocalStorageService localStorage)
    {
        IEnumerable<string> keys = await localStorage.GetKeysWithPrefix(LocalStoragePaths.LightConfigurationProfilePrefix);
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
        await foreach (LightConfigurationProfile loaded in LoadAll(localStorage))
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
        return profile ?? await GetOrCreateDefaultInstance(localStorage);
    }

    public static async Task SaveAsCurrent(ILocalStorageService localStorage, LightConfigurationProfile profile)
    {
        await Save(localStorage, profile);
        await localStorage.SetItemAsStringAsync(LocalStoragePaths.LightConfigurationCurrent, profile.localStoragePath);
    }

    private static ValueTask<LightConfigurationProfile?> Load(ILocalStorageService localStorage, string key)
    {
        return localStorage.GetItemAsync<LightConfigurationProfile>(key);
    }
    public static ValueTask Save(ILocalStorageService localStorage, LightConfigurationProfile profile)
    {
        return localStorage.SetItemAsync(profile.localStoragePath, profile);
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
            handlers: new List<LightHandler>()
            {
                new ScreenLightHandler(null)
            }
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
