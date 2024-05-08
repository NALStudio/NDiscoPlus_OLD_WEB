using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Spotify;

internal sealed class SpotifyToken
{
    [JsonRequired, JsonPropertyName("access_token")]
    public string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; }

    [JsonRequired, JsonPropertyName("scope")]
    public string Scope { get; init; }

    [JsonRequired, JsonPropertyName("expires_in")]
    public int ExpiresInSeconds { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    public SpotifyToken(string accessToken, string tokenType, string scope, int expiresInSeconds, string refreshToken)
    {
        AccessToken = accessToken;
        TokenType = tokenType;
        Scope = scope;
        ExpiresInSeconds = expiresInSeconds;
        RefreshToken = refreshToken;
    }

    public AccessToken ToAccessToken()
    {
        return new AccessToken(
            AccessToken,
            TokenType,
            new TimeSpan(hours: 0, minutes: 0, seconds: ExpiresInSeconds),
            scope: Scope.Split(' ').ToFrozenSet()
        );
    }
}

internal class SpotifyTokenFetchError : Exception
{
    public SpotifyTokenFetchError() : base()
    {
    }

    public SpotifyTokenFetchError(string? message) : base(message)
    {
    }

    public SpotifyTokenFetchError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

internal class SpotifyAuthorization
{
    public string ClientId { get; init; }
    public string ClientSercret { get; init; }

    public SpotifyAuthorization(string clientId, string clientSercret)
    {
        ClientId = clientId;
        ClientSercret = clientSercret;
    }
}

internal class SpotifyTokenClient
{
    public HttpClient HttpClient { get; init; }

    public SpotifyTokenClient()
    {
        HttpClient = new HttpClient();
    }

    public SpotifyTokenClient(HttpClient client)
    {
        HttpClient = client;
    }

    public Task<SpotifyToken> AuthorizeAsync(SpotifyAuthorization authorization, string authorizationCode, string redirectUri)
    {
        return _FetchAsync(
            authorization,
            new Dictionary<string, string> {
                { "grant_type", "authorization_code" },
                { "code", authorizationCode },
                { "redirect_uri", redirectUri },
            });
    }

    public Task<SpotifyToken> RefreshAsync(string refreshToken)
    {
        return _FetchAsync(
            null,
            new Dictionary<string, string> {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
            });
    }

    async Task<SpotifyToken> _FetchAsync(SpotifyAuthorization? authorization, Dictionary<string, string> queryParameters, int maxRetries = 5)
    {
        string queryParams = string.Join('&', queryParameters.Select(kv => $"{kv.Key}={kv.Value}"));
        HttpRequestMessage request = new(HttpMethod.Post, queryParams);
        request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

        if (authorization != null)
        {
            string unencoded = $"{authorization.ClientId}:{authorization.ClientSercret}";
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(unencoded));
            request.Headers.Add("Authorization", $"Basic {encoded}");
        }


        for (int i = 0; i < maxRetries; i++)
        {
            SpotifyToken? token = await __TryFetchAsync(request);
            if (token != null)
                return token;
        }

        throw new SpotifyTokenFetchError($"Token could not be successfully fetched in {maxRetries} tries.");
    }

    async Task<SpotifyToken?> __TryFetchAsync(HttpRequestMessage request)
    {
        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        Stream body = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<SpotifyToken>(body);
    }
}
