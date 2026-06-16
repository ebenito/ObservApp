using ObservApp.Shared.Services;

namespace ObservApp.Web.Services;

/// <summary>
/// Stub de <see cref="ITextToSpeechService"/> para el lado servidor (SSR).
/// No hay síntesis de voz disponible en el servidor; la lectura real
/// ocurre tras la hidratación en el cliente WASM
/// (<see cref="ObservApp.Web.Client.Services.WebTextToSpeechService"/>).
/// </summary>
public sealed class SsrTextToSpeechService : ITextToSpeechService
{
	public event Action? PlaybackStateChanged
	{
		add { }
		remove { }
	}

	public bool IsSpeaking => false;

	public Task SpeakAsync(string text, string languageCode) => Task.CompletedTask;

	public void Stop() { }
}