namespace ObservApp.Shared.Services;

/// <summary>
/// Servicio de síntesis de voz (Text-To-Speech) genérico, multiplataforma.
/// A diferencia de <see cref="IEclipseAudioService"/> (que tiene fallback a
/// beep para avisos de eclipse), este servicio está pensado para leer
/// contenido textual (p. ej. artículos de "Señales") en el idioma del
/// propio contenido, no en el idioma activo de la UI.
/// </summary>
public interface ITextToSpeechService
{
	/// <summary>
	/// Se dispara cuando cambia <see cref="IsSpeaking"/>, ya sea porque
	/// la lectura termina de forma natural, se detiene manualmente, o
	/// falla. Permite a la UI actualizar el estado de los botones.
	/// </summary>
	event Action? PlaybackStateChanged;

	/// <summary>True si hay una lectura en curso actualmente.</summary>
	bool IsSpeaking { get; }

	/// <summary>
	/// Lee el texto indicado usando una voz para <paramref name="languageCode"/>
	/// (código ISO de 2 letras, p. ej. "es", "en", "fr"). Si ya hay una
	/// lectura en curso (de este u otro texto), se detiene primero para
	/// evitar solapamientos. Si no hay voz disponible para el idioma
	/// solicitado, no reproduce nada (silencio) y <see cref="IsSpeaking"/>
	/// permanece en false.
	/// </summary>
	Task SpeakAsync(string text, string languageCode);

	/// <summary>Detiene la lectura en curso, si la hay.</summary>
	void Stop();
}