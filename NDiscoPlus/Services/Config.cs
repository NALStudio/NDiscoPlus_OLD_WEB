using System.ComponentModel;

namespace NDiscoPlus.Services;
public class Config : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? SpotifyRefreshToken
    {
        get => Preferences.Get("spotify_refresh_token", null);
        set
        {
            Preferences.Set("spotify_refresh_token", value);
            OnPropertyChanged(nameof(SpotifyRefreshToken));
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
