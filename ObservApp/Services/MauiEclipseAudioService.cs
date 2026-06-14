using Microsoft.Maui.Media;
using ObservApp.Shared.Services;
using Plugin.Maui.Audio;

namespace ObservApp.Services;

public sealed class MauiEclipseAudioService : IEclipseAudioService
{
    private readonly ILocalizationService _loc;

    public MauiEclipseAudioService(ILocalizationService loc)
    {
        _loc = loc;
    }

    public bool IsSupported => true;

    public async Task AnnounceEventAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var locale = await GetLocaleAsync();

            // Si GetLocaleAsync no encontró voz para el idioma activo,
            // devuelve null → caemos al fallback de audio
            if (locale is null)
            {
                await PlayBeepAsync(cancellationToken);
                return;
            }

            var settings = new SpeechOptions
            {
                Locale = locale,
                Volume = 1.0f,
                Pitch = 1.0f,
            };
            await TextToSpeech.SpeakAsync(message, settings, cancellationToken);
        }
        catch
        {
            await PlayBeepAsync(cancellationToken);
        }
    }

    public async Task PlayBeepAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Reproduce el .wav empaquetado como MauiAsset
            using var stream = await FileSystem.OpenAppPackageFileAsync("sounds/eclipse-beep.wav");
            var player = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(stream);
            player.Play();
            // Esperar duración aproximada del beep
            await Task.Delay(1500, cancellationToken);
            player.Dispose();
        }
        catch
        {
            // Si no se puede reproducir, silencio
        }
    }

    private async Task<Locale?> GetLocaleAsync()
    {
        var locales = await TextToSpeech.GetLocalesAsync();
        var lang = _loc.CurrentLanguageCode;

        // Solo devuelve voz si hay coincidencia real con el idioma activo
        // Si no hay voz para "ar", devuelve null → fallback a audio
        return locales.FirstOrDefault(l =>
            l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase));
    }
}