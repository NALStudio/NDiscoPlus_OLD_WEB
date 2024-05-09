namespace NDiscoPlus.Services;

// It is stupid to store app secrets in source code
// but as this is a hobby project, I don't have budget for any servers right now

// Everything 'encrypted' in the source files can always be decrypted anyways
// so I don't really bother with encrypting
// as I haven't given any dangerous permissions to these secrets anyways
internal static class Secrets
{
    public const string SpotifyClientId = "3e3bd21c633e4d80ab596c3d38a74903";
    public const string SpotifyClientSercret = "5f33c7d61f314f929c58524381ffc47f";
}
