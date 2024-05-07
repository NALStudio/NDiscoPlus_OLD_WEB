using System.Diagnostics;

namespace NDiscoPlus.Spotify;

class AccessToken
{
    public string Token { get; }
    public string TokenType { get; }
    public TimeSpan ExpiresIn { get; }
    public IReadOnlySet<string> Scope { get; }

    public AccessToken(string token, string tokenType, TimeSpan expiresIn, IReadOnlySet<string> scope)
    {
        Token = token;
        TokenType = tokenType;
        ExpiresIn = expiresIn;
        Scope = scope;
    }

}

internal class SpotifyClient
{
    private readonly HttpClient _http;
    private readonly SpotifyTokenClient _tokenClient;

    private object _accessTokenLock = new();
    private Task? _getAccessToken;
    private AccessToken? _accessToken;

    private string __refreshToken;
    public string RefreshToken
    {
        get => __refreshToken;
        set
        {
            if (__refreshToken != value)
            {
                __refreshToken = value;
                RefreshTokenChanged?.Invoke(this, __refreshToken);
            }
        }
    }
    public event EventHandler<string>? RefreshTokenChanged;

    public SpotifyClient(string refreshToken)
    {
        __refreshToken = refreshToken;

        _http = new HttpClient();
        _tokenClient = new SpotifyTokenClient(_http);
    }

    /// <summary>
    /// Adds some Spotify headers and sends the Spotify request.
    /// Handles some Spotify errors automatically, throws an exception on any unhandled Spotify request errors.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        AccessToken accessToken = await _GetAccessToken();

        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        HttpResponseMessage response = await _http.SendAsync(request);
        switch ((int)response.StatusCode)
        {
            // Handle token expiration
            case 401:
                // TODO: Log access token refresh
                lock (_accessTokenLock)
                {
                    _getAccessToken ??= _RefreshAccessToken();
                }
                await _getAccessToken;
                response = await SendAsync(request); // retry
                break;

            // Handle ratelimit
            case 429:
                string? retryAfter = response.Headers.GetValues("Retry-After").SingleOrDefault();
                int retryAfterMillis;
                string? logMsg; // TODO: Log ratelimit seconds
                if (retryAfter != null)
                {
                    // Retry-After is in seconds
                    retryAfterMillis = int.Parse(retryAfter) * 1000;
                    logMsg = retryAfter;
                }
                else
                {
                    retryAfterMillis = 5000;
                    logMsg = "5 (default)";
                }
                await Task.Delay(retryAfterMillis);
                response = await SendAsync(request);
                break;
        }

        response.EnsureSuccessStatusCode();
        return response;
    }

    async Task<AccessToken> _GetAccessToken()
    {
        lock (_accessTokenLock)
        {
            // if access token isn't null and we aren't loading a new access token
            if (_getAccessToken == null && _accessToken != null)
                return _accessToken;

            _getAccessToken ??= _RefreshAccessToken();
        }

        await _getAccessToken;

        lock (_accessTokenLock)
        {
            Debug.Assert(_accessToken != null);
            return _accessToken;
        }
    }

    async Task _RefreshAccessToken()
    {
        SpotifyToken token = await _tokenClient.RefreshAsync(RefreshToken);
        lock (_accessTokenLock)
        {
            _accessToken = token.ToAccessToken();
            if (token.RefreshToken != null)
                RefreshToken = token.RefreshToken;
        }
    }
}
