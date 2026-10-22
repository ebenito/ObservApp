using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Services;

public class WebAppLifecycleService : IAppLifecycleService
{
    private readonly IJSRuntime _jsRuntime;

    public WebAppLifecycleService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool CanExit => false;

    public async Task ExitApplicationAsync()
    {
        // En la versión web, cerrar la pestaña/ventana del navegador
        await _jsRuntime.InvokeVoidAsync("window.close");
    }
}
