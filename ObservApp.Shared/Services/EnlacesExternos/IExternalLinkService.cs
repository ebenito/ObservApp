namespace ObservApp.Shared.Services;

/// <summary>
/// Servicio para abrir enlaces externos (URLs) de forma segura y
/// multiplataforma. En MAUI delega al navegador del sistema mediante
/// el Launcher nativo; en Web abre una nueva pestaña del navegador.
/// </summary>
public interface IExternalLinkService
{
    /// <summary>
    /// Abre la URL indicada en el navegador externo del sistema (MAUI)
    /// o en una nueva pestaña (Web). No hace nada si la URL es nula,
    /// vacía o no es una URI absoluta válida.
    /// </summary>
    Task OpenAsync(string url);
}
