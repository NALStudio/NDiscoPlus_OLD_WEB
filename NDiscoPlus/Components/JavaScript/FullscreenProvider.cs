using Microsoft.JSInterop;

namespace NDiscoPlus.Components.JavaScript;

public readonly struct WakeLockSentinel
{
    private readonly IJSObjectReference wakeLockSentinel;

    internal WakeLockSentinel(IJSObjectReference wakeLockSentinel)
    {
        this.wakeLockSentinel = wakeLockSentinel;
    }

    public ValueTask Release() => wakeLockSentinel.InvokeVoidAsync("release");
}

public class FullscreenProvider : BaseJSModuleProvider
{
    protected override string ModulePath => "./js/fullscreenProvider.js";
    public FullscreenProvider(IJSRuntime js) : base(js)
    {
    }

    public ValueTask<bool> IsFullscreen => InvokeAsync<bool>("isFullscreen");

    public ValueTask<bool> RequestFullscreen() => InvokeAsync<bool>("requestFullscreen");
    public ValueTask<bool> ExitFullscreen() => InvokeAsync<bool>("exitFullscreen");

    public async ValueTask<WakeLockSentinel> RequestWakeLock()
    {
        IJSObjectReference sentinel = await InvokeAsync<IJSObjectReference>("requestWakeLock");
        return new WakeLockSentinel(sentinel);
    }
}
