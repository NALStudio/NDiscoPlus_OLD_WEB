using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Models;

internal class StoredSpotifyRefreshToken
{
    public string RefreshToken { get; }
    public ImmutableHashSet<string> Scope { get; }

    [JsonConstructor]
    public StoredSpotifyRefreshToken(string refreshToken, ImmutableHashSet<string> scope)
    {
        RefreshToken = refreshToken;
        Scope = scope;
    }

    public StoredSpotifyRefreshToken(string refreshToken, ICollection<string> scope)
    {
        RefreshToken = refreshToken;
        Scope = scope.ToImmutableHashSet();
    }
}
