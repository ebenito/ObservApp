using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// <see cref="IFileSaveService"/> para Blazor WebAssembly: lanza una descarga del navegador
/// (<c>observApp.files.download</c> en <c>location-db.js</c>).
/// </summary>
public sealed class WebFileSaveService : IFileSaveService
{
    private readonly IJSRuntime _js;
    private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

    public WebFileSaveService(IJSRuntime js, IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
    {
        _js = js;
        _l = localizer;
    }

    public string? LastError { get; private set; }

    public async Task<bool> SaveAsync(string fileName, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        LastError = null;
        try
        {
            await _js.InvokeVoidAsync("observApp.files.download", cancellationToken, fileName, contentType, content);
            return true;
        }
        catch (Exception ex)
        {
            LastError = _l["Files_Error_Save", ex.Message];
            return false;
        }
    }
}
