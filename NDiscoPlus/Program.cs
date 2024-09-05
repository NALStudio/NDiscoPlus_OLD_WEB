using Blazored.LocalStorage;
using Blazored.SessionStorage;
using BlazorWorker.Core;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

namespace NDiscoPlus;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddBlazoredSessionStorage();

        builder.Services.AddMudServices();

        // TODO: Wait for Blazor WASM Threads
        // see: https://github.com/dotnet/aspnetcore/issues/17730
        builder.Services.AddWorkerFactory();

        await builder.Build().RunAsync();
    }
}