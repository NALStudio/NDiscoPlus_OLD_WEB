using MauiIcons.Material;
using Microsoft.Extensions.Logging;

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

        return builder.Build();
    }
}
