using CommunityToolkit.Mvvm.ComponentModel;
using NDiscoPlus.Layout;
using NDiscoPlus.Services;

namespace NDiscoPlus.ViewModel;
public partial class HomeViewModel : ObservableObject
{
    private readonly Config config;

    private static readonly SyncView syncView = new();
    private static readonly LoginView loginView = new();
    [ObservableProperty]
    ContentView contentView = syncView;

    public HomeViewModel(Config config)
    {
        this.config = config;
        this.config.PropertyChanged += Config_PropertyChanged;

        SetContent();
    }

    private void Config_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => SetContent();

    void SetContent()
    {
        ContentView = config.SpotifyRefreshToken != null ? syncView : loginView;
    }
}
