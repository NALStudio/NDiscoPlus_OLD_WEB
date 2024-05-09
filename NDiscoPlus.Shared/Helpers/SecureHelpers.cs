using System.Security.Cryptography;

namespace NDiscoPlus.Shared.Helpers;
public static class SecureHelpers
{
    public static string GenerateRandomSpotifyString(int length)
    {
        const string possible = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return RandomNumberGenerator.GetString(possible, length);
    }
}
