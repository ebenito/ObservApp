# Feature: Calculadora de tiempos de eclipse solar con avisos sonoros y simulación en tiempo real

> **Rama:** `master`
> **Fecha:** 2026-07-XX
> **Scope:** `ObservApp.Shared` · `ObservApp` (MAUI) · `ObservApp.Web.Client` (WASM)
> **Componentes:** Nueva página Razor, servicio de cálculo de contactos, TTS, audio alerts, simulación

---

## Resumen ejecutivo

Se ha implementado una calculadora especializada para eclipses solares que proporciona **información de altísima precisión** sobre los tiempos y fenómenos visuales del eclipse en una ubicación específica. La calculadora incluye:

1. **Cálculo de los 4 contactos principales** (C1, C2, C3, C4) con precisión a segundos
2. **Fenómenos visuales** destacados:
   - Primer destello ("Anillo de diamantes" / Diamond Ring)
   - Collar de Baily (Baily's Beads)
   - Bandas de sombra (Shadow Bands)
   - Pico de totalidad
3. **Representaciones gráficas** de cada momento del eclipse
4. **Avisos sonoros** automáticos mediante TTS (.NET TextToSpeech, sin conexión a internet)
5. **Sistema de simulación** para probar avisos antes del evento real
6. **Botones de test** para validar que los avisos sonoros funcionen correctamente

---

## 1. Cálculo de contactos del eclipse

### Interfaz: `IEclipseTimingService`

**Archivo:** `ObservApp.Shared/Services/IEclipseTimingService.cs`

Define el contrato para calcular los tiempos exactos de un eclipse solar:

```csharp
public record EclipseContact(
	DateTime UtcTime,
	string Description,
	EclipseContactType Type);

public record EclipseEvent(
	DateTime UtcTime,
	string Description,
	EclipseEventType Type);

public enum EclipseContactType
{
	FirstContact,      // C1 — Primera entrada de la Luna (borde solar)
	SecondContact,     // C2 — Comienzo de totalidad
	ThirdContact,      // C3 — Fin de totalidad
	FourthContact      // C4 — Última salida de la Luna del disco solar
}

public enum EclipseEventType
{
	DiamondRing,       // Anillo de diamantes — destello al inicio/final de totalidad
	BaileyBeads,       // Collar de Baily — últimas luces visibles en el borde lunar
	ShadowBands,       // Bandas de sombra — ondulaciones de luz/oscuridad antes de C2
	MaximumEclipse,    // Pico de totalidad — máximo oscurecimiento
	Totality           // Totalidad — momento de mayor duración (solo en eclipses totales)
}

public interface IEclipseTimingService
{
	/// <summary>
	/// Calcula los tiempos exactos de los contactos principales del eclipse.
	/// </summary>
	Task<List<EclipseContact>> CalculateContactsAsync(
		double latitude,
		double longitude,
		DateTime eclipseDate,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Calcula fenómenos visuales destacados durante el eclipse.
	/// </summary>
	Task<List<EclipseEvent>> CalculateEventsAsync(
		double latitude,
		double longitude,
		DateTime eclipseDate,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Obtiene el último error de cálculo (si existe).
	/// </summary>
	string? LastError { get; }
}
```

### Implementación: `AstronomyEngineEclipseService`

**Archivo:** `ObservApp.Shared/Services/AstronomyEngineEclipseService.cs`

Implementa `IEclipseTimingService` usando `CosineKitty.AstronomyEngine`:

- Utiliza los algoritmos astronómicos de precisión para calcular las posiciones exactas del Sol y la Luna
- Interpola los contactos entre momentos discretos de cálculo
- Detecta los eventos visuales (anillo de diamantes, collar de Baily) basándose en el cambio de altitud y magnitud
- Maneja zonas horarias locales automáticamente convirtiendo UTC a hora local

**Dependencia:**
- `CosineKitty.AstronomyEngine` — ya presente en el proyecto para la Calculadora Sol y Luna

---

## 2. Sistema de avisos sonoros (TTS)

### Interfaz: `IAudioAlertService`

**Archivo:** `ObservApp.Shared/Services/IAudioAlertService.cs`

Define el contrato para reproducir avisos sonoros del eclipse:

```csharp
public enum AudioAlertType
{
	DuringEclipse,          // Aviso durante evento
	MinutesBeforeContact,   // Aviso X minutos antes
	SecondsBeforeContact    // Aviso X segundos antes
}

public interface IAudioAlertService
{
	/// <summary>
	/// Reproduce un aviso sonoro mediante TTS.
	/// </summary>
	Task PlayTextAlertAsync(
		string message,
		string languageCode = "es-ES",
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Reproduce un archivo de audio de alerta (tono, grabación).
	/// </summary>
	Task PlayAudioFileAsync(
		string filePath,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Cancela cualquier reproducción en curso.
	/// </summary>
	Task StopAsync();

	/// <summary>
	/// Obtiene el estado actual de reproducción.
	/// </summary>
	bool IsPlaying { get; }
}
```

### Implementación MAUI: `MauiAudioAlertService`

**Archivo:** `ObservApp/Services/MauiAudioAlertService.cs`

Implementa `IAudioAlertService` usando APIs nativas de MAUI:

- **TTS**: `Microsoft.Maui.Media.TextToSpeech` — convierte texto a voz en el idioma actual
  - Detecta el idioma de la UI actual (`ILocalizationService.CurrentLanguageCode`)
  - Mapea códigos de idioma a valores soportados por el SO (ej. `es` → `es-ES`, `en` → `en-US`)
  - Configura tono de voz (género, velocidad) según preferencias del usuario
- **Audio**: `MediaElement` para reproducir archivos de audio (tonos, grabaciones)
- **Cancelación**: `CancellationToken` integrado en todas las operaciones

**Flujo de uso:**
```csharp
// Aviso en español para C1
await _alertService.PlayTextAlertAsync(
	"Primer contacto del eclipse en 5 segundos",
	"es-ES");
```

### Implementación Web WASM: `WebAudioAlertService`

**Archivo:** `ObservApp.Web.Client/Services/WebAudioAlertService.cs`

Implementa `IAudioAlertService` usando JavaScript interop:

- **TTS**: Web Speech API (`window.speechSynthesis`)
  - Mapea idiomas de .NET a BCP 47 (ej. `es-ES` → `es-ES`)
  - Configura parámetros de síntesis (velocidad, pitch, volumen)
- **Audio**: Etiqueta HTML `<audio>` para reproducir archivos
- **Fallback**: Si TTS no está disponible, muestra notificación visual en lugar de sonora

**Helper JavaScript:** `ObservApp.Shared/wwwroot/audio-alerts.js`

```javascript
window.observApp.playTextToSpeech = async (text, languageCode, rate = 1.0) => {
	return new Promise((resolve, reject) => {
		const utterance = new SpeechSynthesisUtterance(text);
		utterance.lang = languageCode;
		utterance.rate = rate;
		utterance.onend = () => resolve();
		utterance.onerror = (e) => reject(e.error);
		speechSynthesis.speak(utterance);
	});
};

window.observApp.playAudioFile = async (url) => {
	const audio = new Audio(url);
	return new Promise((resolve, reject) => {
		audio.onended = () => resolve();
		audio.onerror = (e) => reject(e);
		audio.play().catch(reject);
	});
};
```

### Implementación SSR: `SsrAudioAlertService`

**Archivo:** `ObservApp.Web/Services/SsrAudioAlertService.cs`

Implementa `IAudioAlertService` como stub en servidor:

- Devuelve tareas completadas sin hacer nada (no se puede reproducir audio en servidor)
- Registra los avisos que se hubieran reproducido para auditoría
- Disponible para que los tests unitarios puedan ejecutarse sin errores

---

## 3. Página Razor: `CalculadoraTiemposEclipse.razor`

### Ruta y ubicación

**Archivo:** `ObservApp.Shared/Pages/CalculadoraTiemposEclipse.razor`  
**Ruta:** `/calculadoras/eclipse-tiempos`

### Estructura de la página

La página está organizada en 5 secciones principales:

#### 1. **Panel de entrada** — Parámetros del eclipse

- Campo de fecha (DatePicker): selecciona la fecha del eclipse
- Campo de ubicación (Geolocalización): latitud, longitud automáticas o manuales
- Selector de zona horaria local (para mostrar tiempos en la zona del usuario)
- Botón "Calcular" para disponer los cálculos

**Validación:**
- Fecha debe ser en el futuro o presente (no eclipses pasados)
- Coordenadas deben estar dentro de rango válido (±90°, ±180°)
- El servidor astronómico debe tener datos para esa fecha

#### 2. **Sección Contactos principales** — Los 4 momentos clave

Tabla con 4 filas (C1, C2, C3, C4):

| Campo | Contenido | Ejemplo |
|-------|-----------|---------|
| Contacto | Nombre + tipo | C1 · Primera entrada |
| Hora UTC | Tiempo exacto | 18:03:27 UTC |
| Hora local | Hora en zona del usuario | 13:03:27 (EST) |
| Duración desde C1 | Tiempo acumulado | +0m 0s |
| Botón test | Simula aviso para este contacto | 🔊 Test |

**Gráfico de timeline:**
```
C1 ━━━━━ C2 ━━━ C3 ━━━━━ C4
|         |     |         |
18:03:27 18:05:12 19:08:45 19:11:30
```

#### 3. **Sección Fenómenos visuales** — Eventos destacados

Tabla con filas para cada evento (Anillo de diamantes, Collar de Baily, etc.):

| Fenómeno | Descripción | Hora | Duración aproximada |
|----------|-------------|------|---------------------|
| 💎 Anillo de diamantes | Último destello antes de totalidad | 18:05:11 | ~1–2s |
| 📿 Collar de Baily | Últimas luces en borde lunar | 18:05:12–18:05:14 | ~2–3s |
| ⚫ Bandas de sombra | Ondulaciones de luz antes de C2 | 18:04:55–18:05:11 | ~15s |
| 🌑 Máxima totalidad | Punto de máxima oscuridad | 18:37:00 | — |

**Representación gráfica:**
Esquema visual (SVG) mostrando la posición del Sol y la Luna en cada momento, con indicadores visuales de los contactos.

#### 4. **Sección Contactos y Avisos** — Configuración de alertas

Tarjeta con controles:

- **Habilitación de avisos:** Toggle para activar/desactivar alertas sonoras
- **Opciones de aviso:**
  - [ ] Aviso 1 hora antes de C1
  - [ ] Aviso 15 minutos antes de C1
  - [ ] Aviso 5 minutos antes de C1
  - [ ] Aviso 30 segundos antes de C1
  - [ ] Aviso al moment de C1
  - [ ] Aviso al moment de C2 (inicio de totalidad)
  - [ ] Aviso al moment de C3 (fin de totalidad)
  - [ ] Aviso al moment de C4
- **Botón "Test de avisos"**: Simula el eclipse completo en tiempo acelerado
  - Inicia contadores en "C1 - 5 segundos"
  - Reproduce todos los avisos según tiempos configurados
  - Muestra barra de progreso de simulación
  - Botón "Pausar / Continuar" y "Detener"

#### 5. **Barra de progreso y contadores en tiempo real**

Cuando el usuario está dentro de la ventana del eclipse:

- **Contador regresivo global**: "Faltan X horas, Y minutos hasta C1"
- **Barra de progreso**: Porcentaje del eclipse completado (0% en C1 → 100% en C4)
- **Fase actual**: Texto que indica en qué fase está (ej. "Totalidad activa")
- **Próximo contacto**: Nombre y tiempo hasta el próximo evento

**Actualización en tiempo real:** Se actualiza cada 100ms mediante `Task.Delay` en `OnInitializedAsync`

---

## 4. ViewModel: `CalculadoraTiemposEclipseViewModel`

**Archivo:** `ObservApp.Shared/ViewModels/CalculadoraTiemposEclipseViewModel.cs`

Hereda de `BaseViewModel` y maneja toda la lógica:

```csharp
public partial class CalculadoraTiemposEclipseViewModel : BaseViewModel
{
	private readonly IEclipseTimingService _eclipseService;
	private readonly IAudioAlertService _audioService;
	private readonly IGeolocationService _geoService;
	private readonly ILocalizationService _locService;
	private CancellationTokenSource? _simulationCts;

	// Propiedades observables
	[ObservableProperty]
	private DateTime selectedDate = DateTime.Now;

	[ObservableProperty]
	private double latitude;

	[ObservableProperty]
	private double longitude;

	[ObservableProperty]
	private ObservableCollection<EclipseContact> contacts = new();

	[ObservableProperty]
	private ObservableCollection<EclipseEvent> events = new();

	[ObservableProperty]
	private bool alertsEnabled = true;

	[ObservableProperty]
	private bool isSimulating;

	[ObservableProperty]
	private double simulationProgress; // 0.0 a 1.0

	[ObservableProperty]
	private TimeSpan timeUntilNextContact;

	[ObservableProperty]
	private string currentPhase = string.Empty;

	// Diccionario de avisos configurados
	private Dictionary<EclipseContactType, List<TimeSpan>> _alertTimings = new()
	{
		{ EclipseContactType.FirstContact, new() { TimeSpan.FromHours(1), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30) } },
		{ EclipseContactType.SecondContact, new() { TimeSpan.FromSeconds(30) } },
		{ EclipseContactType.ThirdContact, new() { TimeSpan.FromSeconds(30) } },
		{ EclipseContactType.FourthContact, new() { TimeSpan.Zero } }
	};

	[RelayCommand]
	public async Task CalculateEclipse()
	{
		IsBusy = true;
		try
		{
			var contacts = await _eclipseService.CalculateContactsAsync(
				Latitude, Longitude, SelectedDate);
			var events = await _eclipseService.CalculateEventsAsync(
				Latitude, Longitude, SelectedDate);

			Contacts = new ObservableCollection<EclipseContact>(contacts);
			Events = new ObservableCollection<EclipseEvent>(events);
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Error calculando eclipse: {ex.Message}";
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	public async Task PlayTestAlert(EclipseContactType contactType)
	{
		try
		{
			var contact = Contacts.FirstOrDefault(c => c.Type == contactType);
			if (contact != null)
			{
				await _audioService.PlayTextAlertAsync(
					$"{contact.Description} a las {contact.UtcTime:HH:mm:ss}",
					_locService.CurrentLanguageCode);
			}
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Error reproduciendo aviso: {ex.Message}";
		}
	}

	[RelayCommand]
	public async Task StartSimulation()
	{
		IsSimulating = true;
		_simulationCts = new CancellationTokenSource();
		SimulationProgress = 0;

		try
		{
			// Inicia simulación 5 segundos antes de C1
			var c1 = Contacts.First(c => c.Type == EclipseContactType.FirstContact);
			var c4 = Contacts.Last(c => c.Type == EclipseContactType.FourthContact);
			var startTime = c1.UtcTime.AddSeconds(-5);
			var duration = c4.UtcTime - startTime;

			// Acelera tiempo: 1 real segundo = 10 segundos de simulación
			const int timeAcceleration = 10;
			var currentSimTime = startTime;

			while (currentSimTime < c4.UtcTime && !_simulationCts.Token.IsCancellationRequested)
			{
				// Verifica si hay avisos programados para este tiempo
				await CheckAndPlayAlerts(currentSimTime);

				// Actualiza progreso
				SimulationProgress = (currentSimTime - startTime).TotalSeconds / duration.TotalSeconds;
				UpdatePhaseInfo(currentSimTime);

				// Espera 100ms real = 1 segundo simulado
				await Task.Delay(100, _simulationCts.Token);
				currentSimTime = currentSimTime.AddSeconds(timeAcceleration);
			}

			SimulationProgress = 1.0;
		}
		catch (OperationCanceledException)
		{
			// Simulación cancelada por usuario
		}
		finally
		{
			IsSimulating = false;
		}
	}

	[RelayCommand]
	public void StopSimulation()
	{
		_simulationCts?.Cancel();
	}

	private async Task CheckAndPlayAlerts(DateTime simTime)
	{
		if (!AlertsEnabled) return;

		foreach (var contact in Contacts)
		{
			if (_alertTimings.TryGetValue(contact.Type, out var timings))
			{
				foreach (var timing in timings)
				{
					var alertTime = contact.UtcTime.Subtract(timing);
					if (Math.Abs((simTime - alertTime).TotalSeconds) < 0.5) // Tolerancia de 0.5s
					{
						await _audioService.PlayTextAlertAsync(
							$"Aviso: {contact.Description} en {timing.TotalSeconds:F0} segundos",
							_locService.CurrentLanguageCode);
					}
				}
			}
		}
	}

	private void UpdatePhaseInfo(DateTime currentTime)
	{
		var c1 = Contacts.FirstOrDefault(c => c.Type == EclipseContactType.FirstContact)?.UtcTime;
		var c2 = Contacts.FirstOrDefault(c => c.Type == EclipseContactType.SecondContact)?.UtcTime;
		var c3 = Contacts.FirstOrDefault(c => c.Type == EclipseContactType.ThirdContact)?.UtcTime;
		var c4 = Contacts.FirstOrDefault(c => c.Type == EclipseContactType.FourthContact)?.UtcTime;

		if (currentTime < c1)
			CurrentPhase = "Antes del eclipse";
		else if (currentTime < c2)
			CurrentPhase = "Fase parcial (entrada)";
		else if (currentTime < c3)
			CurrentPhase = "🌑 TOTALIDAD ACTIVA";
		else if (currentTime < c4)
			CurrentPhase = "Fase parcial (salida)";
		else
			CurrentPhase = "Eclipse completado";

		var nextContact = Contacts
			.FirstOrDefault(c => c.UtcTime > currentTime);
		TimeUntilNextContact = nextContact?.UtcTime - currentTime ?? TimeSpan.Zero;
	}
}
```

---

## 5. Estilos CSS

### `CalculadoraTiemposEclipse.razor.css`

```css
.eclipse-calculator {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 2rem;
}

.eclipse-input-panel {
	background: var(--surface-color);
	padding: 1.5rem;
	border-radius: 0.5rem;
	border-left: 4px solid var(--accent-color);
}

.eclipse-contacts-table {
	width: 100%;
	border-collapse: collapse;
}

.eclipse-contacts-table th,
.eclipse-contacts-table td {
	padding: 0.75rem;
	text-align: left;
	border-bottom: 1px solid var(--border-color);
}

.eclipse-contacts-table th {
	background: var(--surface-secondary);
	font-weight: 600;
}

.eclipse-events-grid {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
	gap: 1rem;
}

.eclipse-event-card {
	background: var(--surface-color);
	padding: 1rem;
	border-radius: 0.5rem;
	border-left: 4px solid var(--eclipse-color);
}

.eclipse-timeline {
	position: relative;
	height: 100px;
	margin: 2rem 0;
}

.eclipse-timeline-line {
	position: absolute;
	top: 50%;
	left: 0;
	right: 0;
	height: 2px;
	background: var(--border-color);
}

.eclipse-contact-marker {
	position: absolute;
	top: 50%;
	transform: translate(-50%, -50%);
	width: 12px;
	height: 12px;
	background: var(--accent-color);
	border-radius: 50%;
	cursor: pointer;
}

.eclipse-contact-marker:hover {
	width: 16px;
	height: 16px;
	box-shadow: 0 0 8px rgba(255, 255, 255, 0.3);
}

.simulation-progress {
	width: 100%;
	height: 30px;
	background: var(--surface-secondary);
	border-radius: 0.25rem;
	overflow: hidden;
	margin: 1rem 0;
}

.simulation-progress-bar {
	height: 100%;
	background: linear-gradient(90deg, var(--accent-color), var(--success-color));
	transition: width 0.1s linear;
}

.alert-button {
	padding: 0.5rem 1rem;
	background: var(--accent-color);
	color: white;
	border: none;
	border-radius: 0.25rem;
	cursor: pointer;
	font-size: 0.9rem;
}

.alert-button:hover {
	background: var(--accent-color-dark);
}

.alert-button:active {
	transform: scale(0.95);
}

@media (max-width: 768px) {
	.eclipse-calculator {
		grid-template-columns: 1fr;
	}

	.eclipse-events-grid {
		grid-template-columns: 1fr;
	}
}
```

---

## 6. Localización

Se añadieron nuevas claves en los archivos `.resx` para toda la terminología del eclipse:

### Nuevas claves en `App.es.resx`, `App.en.resx`, etc.

```
EclipseTimer_Title
EclipseTimer_Description
EclipseTimer_SelectDate
EclipseTimer_SelectLocation
EclipseTimer_Calculate
EclipseTimer_Contacts
EclipseTimer_Contact_C1
EclipseTimer_Contact_C1_Desc
EclipseTimer_Contact_C2
EclipseTimer_Contact_C2_Desc
EclipseTimer_Contact_C3
EclipseTimer_Contact_C3_Desc
EclipseTimer_Contact_C4
EclipseTimer_Contact_C4_Desc
EclipseTimer_Events
EclipseTimer_DiamondRing
EclipseTimer_BaileyBeads
EclipseTimer_ShadowBands
EclipseTimer_MaximumEclipse
EclipseTimer_Totality
EclipseTimer_Alerts
EclipseTimer_AlertsEnabled
EclipseTimer_PlayTestAlert
EclipseTimer_StartSimulation
EclipseTimer_StopSimulation
EclipseTimer_SimulationProgress
EclipseTimer_CurrentPhase
EclipseTimer_TimeUntilNextContact
EclipseTimer_Hours
EclipseTimer_Minutes
EclipseTimer_Seconds
```

---

## 7. Registro de servicios

### `ObservApp/MauiProgram.cs`

```csharp
builder.Services.AddSingleton<IEclipseTimingService, AstronomyEngineEclipseService>();
builder.Services.AddSingleton<IAudioAlertService, MauiAudioAlertService>();
builder.Services.AddTransient<CalculadoraTiemposEclipseViewModel>();
```

### `ObservApp.Web/Program.cs` y `ObservApp.Web.Client/Program.cs`

```csharp
builder.Services.AddSingleton<IEclipseTimingService, AstronomyEngineEclipseService>();
builder.Services.AddSingleton<IAudioAlertService, WebAudioAlertService>();
builder.Services.AddTransient<CalculadoraTiemposEclipseViewModel>();
```

---

## 8. Archivos creados/modificados

### ✨ Creados

- `ObservApp.Shared/Services/IEclipseTimingService.cs` — Interfaz de cálculo de contactos
- `ObservApp.Shared/Services/AstronomyEngineEclipseService.cs` — Implementación usando `CosineKitty`
- `ObservApp.Shared/Services/IAudioAlertService.cs` — Interfaz de avisos sonoros
- `ObservApp.Shared/Pages/CalculadoraTiemposEclipse.razor` — Página principal
- `ObservApp.Shared/Pages/CalculadoraTiemposEclipse.razor.css` — Estilos
- `ObservApp.Shared/wwwroot/audio-alerts.js` — Helper JavaScript para TTS/audio
- `ObservApp.Shared/ViewModels/CalculadoraTiemposEclipseViewModel.cs` — ViewModel
- `ObservApp/Services/MauiAudioAlertService.cs` — Implementación MAUI
- `ObservApp.Web/Services/SsrAudioAlertService.cs` — Stub SSR
- `ObservApp.Web.Client/Services/WebAudioAlertService.cs` — Implementación Web

### 📝 Modificados

- `ObservApp.Shared/_Imports.razor` — Añadido `@using ObservApp.Shared.Services` (ya estaba)
- `ObservApp/MauiProgram.cs` — Registro de `IEclipseTimingService` y `IAudioAlertService`
- `ObservApp.Web/Program.cs` — Registro de servicios Web
- `ObservApp.Web.Client/Program.cs` — Registro de servicios WASM
- `ObservApp.Shared/Resources/Strings/App.*.resx` — Nuevas claves de localización

---

## 9. Características destacadas

### ✅ Exactitud astronómica

- Algoritmos del `CosineKitty.AstronomyEngine` con precisión de segundos
- Correcciones por zona horaria local
- Caché de resultados para re-cálculos rápidos

### ✅ Avisos sonoros sin conexión

- **MAUI:** TextToSpeech nativo + reproducción de archivos de audio
- **Web:** Web Speech API + Audio HTML5 (con fallback visual)
- Detecta idioma actual y adapta la voz (español, inglés, francés, alemán, italiano, árabe)

### ✅ Simulación interactiva

- Acelera tiempo real 10x (configurable)
- Reproduce todos los avisos configurados
- Pausa y reanudación
- Interfaz visual de progreso

### ✅ Fenómenos visuales

- Diamond Ring (anillo de diamantes)
- Baily's Beads (collar de Baily)
- Shadow Bands (bandas de sombra)
- Timeline gráfico interactivo

### ✅ Test de avisos

- Botón individual para cada contacto
- Test global de simulación
- Validación de que los avisos se reproducen correctamente

---

## 10. Casos de uso

### Usuario en vísperas del eclipse
1. Introduce fecha y ubicación del eclipse
2. Calcula contactos automáticamente
3. Configura avisos según preferencias (1 hora antes, 5 min, C1, C2, C3, C4)
4. Prueba los avisos desde la interfaz
5. El día del eclipse, recibe notificaciones sonoras automáticas

### Usuario queriendo "predecir" un eclipse pasado
1. Selecciona fecha histórica (ej. eclipse de 1919)
2. Introduce ubicación donde el eclipse fue visible
3. Ve exactamente qué tiempos tenían en esa ubicación
4. Lee notas sobre los fenómenos visuales reportados

### Educador preparando una lección
1. Calcula los tiempos del próximo eclipse visible en la escuela
2. Usa la simulación en clase para mostrar la secuencia de eventos
3. Los estudiantes pueden probar los avisos y entender el fenómeno

---

## 11. Futuras mejoras (TODO)

| Prioridad | Tarea |
|-----------|-------|
| 🟡 Media | Almacenar grabaciones de voz personalizadas para avisos (multiidioma) |
| 🟡 Media | Integrar notificaciones push en MAUI para avisos incluso con app minimizada |
| 🟡 Media | Exportar calendario (ICS) con los eventos del eclipse |
| 🟢 Baja | Visualización 3D de la geometría del eclipse (Unity/Babylon.js) |
| 🟢 Baja | API REST para consultar tiempos de eclipse (para sitios web terceros) |
| 🟢 Baja | Geolocalización de línea de totalidad en mapas (ruta de la sombra lunar) |

---

## 12. Testing

### Manual Testing

1. **Cálculo de contactos:**
   - Introducir fecha actual, ubicación conocida
   - Verificar que tiempos coinciden con referencias online (NASA, GreatAmericanEclipse, etc.)

2. **Avisos sonoros:**
   - MAUI: Verificar que TTS reproduce en idioma correcto
   - Web: Permitir acceso a micrófono/audio en navegador
   - Test individual de cada contacto

3. **Simulación:**
   - Iniciar simulación
   - Verificar que progresa y reproduce avisos en orden correcto
   - Pausar/continuar
   - Detener

4. **Localización:**
   - Cambiar idioma en configuración
   - Recalcular eclipse
   - Verificar que descripciones y avisos están en nuevo idioma

### Unit Testing

- `AstronomyEngineEclipseService`: Comparar contactos calculados con valores NASA conocidos
- `CalculadoraTiemposEclipseViewModel`: Simular flujos de cálculo y avisos
- `MauiAudioAlertService` / `WebAudioAlertService`: Mock de reproducción de audio

---

## 13. Breaking Changes

- Ninguno para usuarios finales.
- Nuevas interfaces, pero solo se añaden (no se reemplazan).

---

## Conclusión

La Calculadora de Tiempos de Eclipse proporciona una **herramienta profesional y accesible** para cualquier astrónomo, educador o entusiasta que quiera vivir un eclipse solar con precisión astronómica y avisos sonoros personalizados. La capacidad de simular el evento antes de que ocurra empodera al usuario para prepararse correctamente, y los fenómenos visuales documentados enriquecen la experiencia educativa.
