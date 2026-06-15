using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// Implementación de <see cref="ITextToSpeechService"/> para Blazor WebAssembly.
/// Usa la Web Speech API (window.speechSynthesis) con una voz que coincida
/// con el idioma del texto a leer, independientemente del idioma activo
/// de la UI.
/// </summary>
public sealed class WebTextToSpeechService : ITextToSpeechService, IDisposable
{
	private readonly IJSRuntime _js;
	private DotNetObjectReference<WebTextToSpeechService>? _selfRef;

	public WebTextToSpeechService(IJSRuntime js)
	{
		_js = js;
	}

	public event Action? PlaybackStateChanged;

	public bool IsSpeaking { get; private set; }

	public async Task SpeakAsync(string text, string languageCode)
	{
		// Detener cualquier lectura previa antes de empezar una nueva.
		Stop();

		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(languageCode))
			return;

		_selfRef ??= DotNetObjectReference.Create(this);

		bool started;
		try
		{
			started = await _js.InvokeAsync<bool>(
				"observApp.speakWithLanguage",
				text, languageCode, _selfRef);
		}
		catch
		{
			started = false;
		}

		if (started)
			SetSpeaking(true);
		// Si no hay voz disponible (started == false): silencio, sin cambiar estado.
	}

	public void Stop()
	{
		if (!IsSpeaking) return;

		try
		{
			_ = _js.InvokeVoidAsync("observApp.stopSpeaking");
		}
		catch { /* silencio */ }

		SetSpeaking(false);
	}

	/// <summary>
	/// Invocado desde JS cuando la utterance termina o falla
	/// (evento onend/onerror de SpeechSynthesisUtterance).
	/// </summary>
	[JSInvokable]
	public void OnSpeechEnded()
	{
		SetSpeaking(false);
	}

	private void SetSpeaking(bool value)
	{
		if (IsSpeaking == value) return;
		IsSpeaking = value;
		PlaybackStateChanged?.Invoke();
	}

	public void Dispose()
	{
		_selfRef?.Dispose();
	}
}