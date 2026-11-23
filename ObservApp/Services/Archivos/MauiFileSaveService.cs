using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Localization;
using ObservApp.Shared.Services;

namespace ObservApp.Services;

/// <summary>
/// <see cref="IFileSaveService"/> para MAUI: abre el diálogo nativo "Guardar como"
/// (Android: Storage Access Framework, sin permisos extra; Windows: FileSavePicker).
/// </summary>
public sealed class MauiFileSaveService : IFileSaveService
{
    private readonly IFileSaver _fileSaver;
    private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

    public MauiFileSaveService(IFileSaver fileSaver, IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
    {
        _fileSaver = fileSaver;
        _l = localizer;
    }

    public string? LastError { get; private set; }

    public async Task<bool> SaveAsync(string fileName, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        LastError = null;
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            var result = await _fileSaver.SaveAsync(fileName, stream, cancellationToken);

            if (result.IsSuccessful)
            {
                return true;
            }

            // Cancelar el diálogo no es un error que haya que mostrar. El toolkit lo comunica
            // como FileSaveException("Operation cancelled.") en Android y Windows.
            if (result.Exception is not null && !IsCancellation(result.Exception))
            {
                LastError = _l["Files_Error_Save", result.Exception.Message];
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            LastError = _l["Files_Error_Save", ex.Message];
            return false;
        }
    }

    private static bool IsCancellation(Exception ex) =>
        ex is OperationCanceledException ||
        (ex is FileSaveException && ex.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase));
}
