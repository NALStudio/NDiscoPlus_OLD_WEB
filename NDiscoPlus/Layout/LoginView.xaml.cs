using Microsoft.AspNetCore.WebUtilities;
using NDiscoPlus.Services;
using NDiscoPlus.Shared.Helpers;

namespace NDiscoPlus.Layout;

public partial class LoginView : ContentView
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void Button_Pressed(object sender, EventArgs e)
    {
        try
        {
            string url = QueryHelpers.AddQueryString(
                "https://accounts.spotify.com/authorize",
                new Dictionary<string, string?>
                {
                    { "response_type", "code" },
                    { "client_id", Secrets.SpotifyClientId },
                    { "scope", string.Join(' ', SpotifyConstants.Scope) },
                    { "redirect_uri", SpotifyConstants.RedirectUri },
                    { "state", SecureHelpers.GenerateRandomSpotifyString(16) }
                }
            );
            _ = Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            _ = Application.Current?.MainPage?.DisplayAlert("Login Failed!", ex.Message, "Ok");
        }
    }
}