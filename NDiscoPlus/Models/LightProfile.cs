using Blazored.LocalStorage;
using MemoryPack;
using NDiscoPlus.Constants;
using NDiscoPlus.LightHandlers;
using NDiscoPlus.LightHandlers.Screen;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Models;

public sealed class LightProfile
{
    private class SerializableProfile
    {
        public string Name { get; }
        public ImmutableArray<LightHandlerConfig> Handlers { get; }
        public ImmutableDictionary<string, LightConfig> LightConfigurationOverrides { get; }

        [JsonConstructor]
        private SerializableProfile(string name, ImmutableArray<LightHandlerConfig> handlers, ImmutableDictionary<string, LightConfig>? lightConfigurationOverrides)
        {
            Name = name;
            Handlers = handlers;
            LightConfigurationOverrides = lightConfigurationOverrides ?? ImmutableDictionary<string, LightConfig>.Empty; // empty dictionary for backwards compat
        }

        public static SerializableProfile Construct(LightProfile profile)
        {
            ImmutableArray<LightHandlerConfig> handlers = profile.handlers.Select(h => h.ConfigRef).ToImmutableArray();

            return new SerializableProfile(
                profile.Name,
                handlers: handlers,
                lightConfigurationOverrides: profile.LightConfigurationOverrides.ToImmutableDictionary(key => SerializeLightId(key.Key), value => value.Value)
            );
        }

        public static LightProfile Deconstruct(string localStoragePath, SerializableProfile profile)
        {
            return new LightProfile(
                localStoragePath: localStoragePath,
                name: profile.Name,
                handlers: profile.Handlers.Select(h => h.CreateLightHandler()),
                lightConfigurationOverrides: profile.LightConfigurationOverrides.Select(x => new KeyValuePair<LightId, LightConfig>(DeserializeLightId(x.Key), x.Value))
            );
        }

        private static string SerializeLightId(LightId value)
            => MemoryPackHelper.SerializeToBase64(value);

        private static LightId DeserializeLightId(string value)
            => MemoryPackHelper.DeserializeFromBase64<LightId>(value) ?? throw new ArgumentException("Could not deserialize value.");
    }

    public class LightConfig
    {
        public Channel Channel { get; set; } = LightRecord.Default.Channel;
        public double Brightness { get; set; } = LightRecord.Default.Brightness;

        public LightRecord CreateRecord(NDPLight light)
        {
            return new(light)
            {
                Channel = Channel,
                Brightness = Brightness,
            };
        }
    }

    private LightProfile(string localStoragePath, string name, IEnumerable<LightHandler> handlers, IEnumerable<KeyValuePair<LightId, LightConfig>> lightConfigurationOverrides)
    {
        this.localStoragePath = localStoragePath;

        Name = name;

        this.handlers = handlers.ToList();
        LightConfigurationOverrides = lightConfigurationOverrides.ToDictionary();
    }

    public string UniqueId => localStoragePath;

    public string Name { get; set; } = string.Empty;

    private readonly string localStoragePath;

    public IList<LightHandler> Handlers => handlers.AsReadOnly();
    private readonly List<LightHandler> handlers;

    public Dictionary<LightId, LightConfig> LightConfigurationOverrides { get; }

    public static async IAsyncEnumerable<LightProfile> LoadAll(ILocalStorageService localStorage, bool _createDefault = true)
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
            LightProfile? profile = await Load(localStorage, key);
            if (profile is not null)
                yield return profile;
            // TODO: Warn if profile could not be deserialized.
        }
    }

    private static async Task<LightProfile> GetOrCreateDefaultInstance(ILocalStorageService localStorage)
    {
        // Try to return the first profile if any are available
        await foreach (LightProfile loaded in LoadAll(localStorage, _createDefault: false))
            return loaded;

        // Create a new default instance and set it as current.
        LightProfile profile = InternalCreateNewWithoutSaving();
        await SaveAsCurrent(localStorage, profile);

        return profile;
    }

    public static async Task<LightProfile> LoadCurrent(ILocalStorageService localStorage)
    {
        string? currentKey = await localStorage.GetItemAsStringAsync(LocalStoragePaths.LightConfigurationCurrent);
        if (string.IsNullOrEmpty(currentKey))
            return await GetOrCreateDefaultInstance(localStorage);

        LightProfile? profile = await Load(localStorage, currentKey);
        if (profile is null)
            return await GetOrCreateDefaultInstance(localStorage);

        return profile;
    }

    public static async Task SaveAsCurrent(ILocalStorageService localStorage, LightProfile profile)
    {
        await Save(localStorage, profile);
        await localStorage.SetItemAsStringAsync(LocalStoragePaths.LightConfigurationCurrent, profile.localStoragePath);
    }

    private static async Task<LightProfile?> Load(ILocalStorageService localStorage, string key)
    {
        SerializableProfile? serializable = await localStorage.GetItemAsync<SerializableProfile>(key);
        if (serializable is null)
            return null;
        return SerializableProfile.Deconstruct(key, serializable);
    }

    public static ValueTask Save(ILocalStorageService localStorage, LightProfile profile)
    {
        SerializableProfile serializable = SerializableProfile.Construct(profile);
        return localStorage.SetItemAsync(profile.localStoragePath, serializable);
    }

    public static async Task<LightProfile> CreateNew(ILocalStorageService localStorage)
    {
        LightProfile profile = InternalCreateNewWithoutSaving();
        await Save(localStorage, profile);
        return profile;
    }

    private static LightProfile InternalCreateNewWithoutSaving()
    {
        string localStoragePath = LocalStoragePaths.LightConfigurationProfilePrefix.NewKey();

        return new LightProfile(
            localStoragePath: localStoragePath,
            name: string.Empty,
            handlers: [new ScreenLightHandler(null)],
            lightConfigurationOverrides: ImmutableDictionary<LightId, LightConfig>.Empty
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

    public IEnumerable<LightRecord> BuildLightRecords(IEnumerable<NDPLight> lights)
    {
        foreach (NDPLight light in lights)
        {
            if (LightConfigurationOverrides.TryGetValue(light.Id, out LightConfig? config))
                yield return config.CreateRecord(light);
            else
                yield return LightRecord.CreateDefault(light);
        }
    }
}