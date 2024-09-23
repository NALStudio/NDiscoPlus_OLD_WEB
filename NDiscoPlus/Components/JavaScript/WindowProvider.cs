using Microsoft.JSInterop;

namespace NDiscoPlus.Components.JavaScript;

public readonly record struct WindowSize(int Width, int Height)
{
    public double AspectRatio => Width / (double)Height;
    public double InverseAspectRatio => Height / (double)Width;
}

public class WindowProvider : BaseJSModuleProvider
{
    private record class JSBridge(WindowProvider Parent)
    {
        [JSInvokable]
        public void OnWindowResized(WindowSize size)
        {
            Parent.OnWindowResize?.Invoke(size);
        }
    }

    protected override string ModulePath => "./js/windowProvider.js";
    public WindowProvider(IJSRuntime js) : base(js)
    {
        _bridge = new(this);
    }

    protected override ValueTask InitializeModule(IJSObjectReference module)
    {
        return module.InvokeVoidAsync("init", DotNetObjectReference.Create(_bridge));
    }

    private readonly JSBridge _bridge;
    public event Action<WindowSize>? OnWindowResize;

    public ValueTask<int> InnerWidth => InvokeAsync<int>("getInnerWidth");
    public ValueTask<int> InnerHeight => InvokeAsync<int>("getInnerHeight");
    public ValueTask<WindowSize> InnerSize => InvokeAsync<WindowSize>("getInnerSize");
}
