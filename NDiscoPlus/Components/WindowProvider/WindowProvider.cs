using Microsoft.JSInterop;

namespace NDiscoPlus.Components.WindowProvider;

public readonly record struct WindowSize(int Width, int Height)
{
    public double AspectRatio => Width / (double)Height;
    public double InverseAspectRatio => Height / (double)Width;
}

public class WindowProvider
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public WindowProvider(IJSRuntime js)
    {
        _js = js;
    }

    public Task<int> InnerWidth => InvokeAsync<int>("getInnerWidth");
    public Task<int> InnerHeight => InvokeAsync<int>("getInnerHeight");
    public Task<WindowSize> InnerSize => InvokeAsync<WindowSize>("getInnerSize");

    private async Task<T> InvokeAsync<T>(string identifier, params object?[]? args)
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/WindowProvider.js");
        return await _module.InvokeAsync<T>(identifier, args);
    }
}
