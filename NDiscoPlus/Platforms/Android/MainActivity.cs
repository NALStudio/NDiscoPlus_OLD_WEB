using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using NDiscoPlus.Services;
using System.Collections.Frozen;

namespace NDiscoPlus;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(actions: [Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = "ndiscoplus")]
public class MainActivity : MauiAppCompatActivity
{
    private readonly AppLinkService? appLinkService;

    public MainActivity()
    {
        appLinkService = IPlatformApplication.Current?.Services.GetService<AppLinkService>();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        OnNewIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        if (appLinkService == null)
            return;

        if (intent == null)
            return;
        if (intent.Action != Intent.ActionView)
            return;

        string? data = intent.DataString;

        if (string.IsNullOrWhiteSpace(data))
            return;

        System.Diagnostics.Trace.Assert(intent.Data is not null);
        string? relativeUri = intent.Data.Path;
        Dictionary<string, StringValues> rawQuery = QueryHelpers.ParseQuery(intent.Data.Query);

        FrozenDictionary<string, string> query = rawQuery.Select(x => KeyValuePair.Create(x.Key, x.Value.Single<string>())).ToFrozenDictionary();

        appLinkService.OnAppLinkReceived(relativeUri, query);
    }
}
