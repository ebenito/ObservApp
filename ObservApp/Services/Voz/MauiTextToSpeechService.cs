using Microsoft.Maui.Media;
using ObservApp.Shared.Services;

namespace ObservApp.Services;

/// <summary>
/// Implementación de <see cref="ITextToSpeechService"/> para .NET MAUI.
/// Usa <see cref="TextToSpeech"/> con la voz que mejor coincida con el
/// idioma del texto a leer (no con el idioma activo de la UI).
/// </summary>
public sealed class MauiTextToSpeechService : ITextToSpeechService
{
    private CancellationTokenSource? _cts;

    public event Action? PlaybackStateChanged;

    public bool IsSpeaking { get; private set; }

    public async Task SpeakAsync(string text, string languageCode)
    {
        // Detener cualquier lectura previa antes de empezar una nueva.
        Stop();

        if (string.IsNullOrWhiteSpace(text))
            return;

        var locale = await FindLocaleAsync(languageCode);
        if (locale is null)
        {
            // Sin voz disponible para este idioma: silencio, sin cambiar estado.
            return;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        SetSpeaking(true);

        try
        {
            var settings = new SpeechOptions
            {
                Locale = locale,
                Volume = 1.0f,
                Pitch  = 1.0f,
            };
            await TextToSpeech.SpeakAsync(text, settings, token);
        }
        catch (OperationCanceledException)
        {
            // Detención manual — esperado.
        }
        catch
        {
            // Error del motor TTS: silencio.
        }
        finally
        {
            // Solo notificar fin si este token sigue siendo el activo
            // (evita carreras si SpeakAsync se llamó de nuevo mientras tanto).
            if (_cts?.Token == token)
                SetSpeaking(false);
        }
    }

    public void Stop()
    {
        if (_cts is null) return;

        try { _cts.Cancel(); }
        catch { /* ya cancelado o liberado */ }

        _cts.Dispose();
        _cts = null;

        SetSpeaking(false);
    }

    private void SetSpeaking(bool value)
    {
        if (IsSpeaking == value) return;
        IsSpeaking = value;
        PlaybackStateChanged?.Invoke();
    }

    private static async Task<Locale?> FindLocaleAsync(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return null;

        var locales = await TextToSpeech.GetLocalesAsync();

        return locales.FirstOrDefault(l =>
            l.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase));
    }
}
