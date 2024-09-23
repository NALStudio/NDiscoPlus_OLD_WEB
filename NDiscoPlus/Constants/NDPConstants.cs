using SpotifyAPI.Web;
using System.Collections.Immutable;

namespace NDiscoPlus.Constants;

internal static class NDPConstants
{
    public const string SpotifyClientId = "3e3bd21c633e4d80ab596c3d38a74903";
    // We don't store the client secret and use PKCE instead for security reasons.

    public static readonly ImmutableArray<string> SpotifyScope = [Scopes.UserReadPlaybackState];

    public const string SpotifyRedirectUri =
#if DEBUG
        "https://localhost:7036/spotify-login";
#else
        "https://nalstudio.github.io/NDiscoPlus/spotify-login";
#endif
}
