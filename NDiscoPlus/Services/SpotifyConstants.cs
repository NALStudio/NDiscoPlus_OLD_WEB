using System.Collections.Frozen;

namespace NDiscoPlus.Services;
internal static class SpotifyConstants
{
    public const string RedirectUri = "ndiscoplus://spotify_login_callback/";
    public static readonly FrozenSet<string> Scope = new HashSet<string>() { "user-read-playback-state", "app-remote-control" }.ToFrozenSet();
}
