using Microsoft.JSInterop;
using System.Diagnostics;
using System.Reflection;

namespace NDiscoPlus.Components.JavaScript;

public abstract class BaseJSModuleProvider
{
    private readonly IJSRuntime js;
    private IJSObjectReference? module;

    protected BaseJSModuleProvider(IJSRuntime js)
    {
        this.js = js;
    }
    protected abstract string ModulePath { get; }

    protected virtual ValueTask InitializeModule(IJSObjectReference module)
    {
        return ValueTask.CompletedTask;
    }

    private async ValueTask<IJSObjectReference> LoadModule()
    {
        IJSObjectReference module = await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
        await InitializeModule(module);
        return module;
    }

    protected async ValueTask InvokeVoidAsync(string identifier, params object?[]? args)
    {
        module ??= await LoadModule();
        await module.InvokeVoidAsync(identifier, args);
    }

    protected async ValueTask<T> InvokeAsync<T>(string identifier, params object?[]? args)
    {
        module ??= await LoadModule();
        return await module.InvokeAsync<T>(identifier, args);
    }
}
