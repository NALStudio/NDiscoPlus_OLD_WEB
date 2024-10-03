using Microsoft.JSInterop;

namespace NDiscoPlus.Components.JavaScript;

public readonly record struct WindowSize(int Width, int Height)
{
    public double AspectRatio => Width / (double)Height;
    public double InverseAspectRatio => Height / (double)Width;
}

public class WindowProvider : BaseJSModuleProvider, IDisposable
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
        JSBridge bridge = new(this);
        _bridge = DotNetObjectReference.Create(bridge);
    }

    protected override ValueTask InitializeModule(IJSObjectReference module)
    {
        return module.InvokeVoidAsync("init", _bridge);
    }

    private readonly DotNetObjectReference<JSBridge> _bridge;

    public event Action<WindowSize>? OnWindowResize;

    public ValueTask<int> InnerWidth => InvokeAsync<int>("getInnerWidth");
    public ValueTask<int> InnerHeight => InvokeAsync<int>("getInnerHeight");
    public ValueTask<WindowSize> InnerSize => InvokeAsync<WindowSize>("getInnerSize");

    public void Dispose()
    {
        _bridge.Dispose();
        GC.SuppressFinalize(this);
    }
}
