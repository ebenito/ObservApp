using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

public class WebClientAppLifecycleService : IAppLifecycleService
{
    private readonly IJSRuntime _jsRuntime;

    public WebClientAppLifecycleService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task ExitApplicationAsync()
    {
        // En la versión web, cerrar la pestaña/ventana del navegador
        await _jsRuntime.InvokeVoidAsync("window.close");
    }
}
