using MauiIcons.Material;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using NDiscoPlus.Page;
using NDiscoPlus.Services;
using NDiscoPlus.ViewModel;

namespace NDiscoPlus;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Roboto-Regular.ttf", "Roboto");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
                fonts.AddFont("Roboto-Italic.ttf", "RobotoItalic");
                fonts.AddFont("Roboto-BoldItalic.ttf", "RobotoBoldItalic");
            }).UseMaterialMauiIcons();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<Config>();
        builder.Services.AddSingleton<AppLinkService>();

        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<HomeViewModel>();

        return builder.Build();
    }
}
