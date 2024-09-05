using Blazored.LocalStorage;
using NDiscoPlus.Shared.Helpers;

namespace NDiscoPlus.Constants;

internal readonly struct StoragePrefix
{
    public readonly string PrefixWithUnderscore;

    public StoragePrefix(string prefix)
    {
        if (prefix.EndsWith('_'))
            throw new ArgumentException("Prefix cannot end with an underscore (_).");

        PrefixWithUnderscore = prefix + "_";
    }

    /// <summary>
    /// Collisions extremely unlikely, but possible.
    /// </summary>
    public string NewKey()
    {
        Guid guid = Guid.NewGuid();
        return PrefixWithUnderscore + guid.ToString("N");
    }

    public static implicit operator StoragePrefix(string value)
        => new(value);
}

internal static class LocalStoragePaths
{
    public const string SpotifyRefreshToken = "spotify-refresh-token";

    public static readonly StoragePrefix LightHandlerConfigPrefix = "light-handler-config";

    public static async Task<IEnumerable<string>> GetKeysWithPrefix(this ILocalStorageService ls, StoragePrefix prefix)
    {
        IEnumerable<string> keys = await ls.KeysAsync();
        return keys.Where(k => k.StartsWith(prefix.PrefixWithUnderscore));
    }
}

internal static class SessionStoragePaths
{
    public const string SpotifyLoginVerifier = "spotify-login-verifier";
}
