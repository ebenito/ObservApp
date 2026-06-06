namespace ObservApp.Shared.Services;

public interface IEclipseAudioService
{
    /// <summary>
    /// Anuncia un evento del eclipse. En MAUI usa TTS; en web reproduce un beep.
    /// </summary>
    Task AnnounceEventAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reproduce solo el sonido de aviso (beep), sin texto.
    /// </summary>
    Task PlayBeepAsync(CancellationToken cancellationToken = default);

    bool IsSupported { get; }
}