using Microsoft.JSInterop;

namespace NDiscoPlus.Components.JavaScript;

public readonly record struct WindowSize(int Width, int Height)
{
    public double AspectRatio => Width / (double)Height;
    public double InverseAspectRatio => Height / (double)Width;
}

public class WindowProvider : BaseJSModuleProvider
{
    protected override string ModulePath => "./js/windowProvider.js";
    public WindowProvider(IJSRuntime js) : base(js)
    {
    }

    public ValueTask<int> InnerWidth => InvokeAsync<int>("getInnerWidth");
    public ValueTask<int> InnerHeight => InvokeAsync<int>("getInnerHeight");
    public ValueTask<WindowSize> InnerSize => InvokeAsync<WindowSize>("getInnerSize");
}
