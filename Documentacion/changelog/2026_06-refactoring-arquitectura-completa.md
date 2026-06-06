# Refactoring: Arquitectura completa con abstracción de servicios, MVVM y geolocalización multiplataforma

> **Rama:** `master`
> **Fecha:** 2026-06-XX
> **Scope:** Solución completa — `ObservApp` · `ObservApp.Web` · `ObservApp.Web.Client` · `ObservApp.Shared`
> **Cambios de API:** Mayores — nuevas interfaces, modelos de dominio, ViewModels, estado global

---

## Resumen ejecutivo

Se ha realizado una refactorización arquitectónica mayor que establece las bases para la escalabilidad y mantenibilidad del proyecto. Los cambios incluyen:

1. **Capa de abstracción de servicios** con interfaces reutilizables para todas las plataformas
2. **Modelos de dominio** para representar sesiones y objetivos de observación
3. **Estado global reactivo** (`AppState`) con patrón Observable
4. **Patrón MVVM** con CommunityToolkit.Mvvm para lógica de negocio
5. **Calculadora Sol y Luna** con cálculos astronómicos offline precisos
6. **Geolocalización multiplataforma** (MAUI + Web + SSR) con interfaz única
7. **Localización dinámica** corregida en Blazor WASM
8. **Router multi-ensamblado** para soportar páginas en hosts futuros
9. **Cierre limpio** en Windows sin forzar `Environment.Exit(0)`

---

## 1. Capa de abstracción de servicios

### `ILocalizationService`
**Archivo:** `ObservApp.Shared/Services/ILocalizationService.cs`

Define el contrato para cambio de idioma dinámico:
- Propiedades: `SupportedLanguages`, `CurrentCulture`, `CurrentLanguageCode`
- Evento: `OnLanguageChanged` — notifica cuando cambia el idioma
- Record exportado: `LanguageOption(Code, Name, Flag)`

**Implementaciones:**
- **MAUI:** `LocalizationService` (modificado para implementar la interfaz, usa `Preferences`)
- **Web WASM:** `WebLocalizationService` (cambia `CultureInfo.DefaultThreadCurrentUICulture`)
- **Web SSR:** Usa la misma implementación Web

### `ISettingsService`
**Archivo:** `ObservApp.Shared/Services/ISettingsService.cs`

Define el contrato para lectura/escritura de preferencias del usuario:
- Métodos: `GetLanguage()`, `SetLanguage(string)`, `GetTheme()`, `SetTheme(string)`

**Implementaciones:**
- **MAUI:** `MauiSettingsService` (nuevo, usa `Preferences` con claves `"app_language"` y `"app_theme"`)
- **Web WASM:** `WebSettingsService` (nuevo, actualizado a usar `localStorage` vía JSInterop)
- **Web SSR:** `WebSettingsService` (mismo cliente que WASM, persistencia por sesión)

### `IGeolocationService`
**Archivo:** `ObservApp.Shared/Services/IGeolocationService.cs`

Define el contrato para obtener ubicación del dispositivo:
- Record exportado: `LocationData(Latitude, Longitude, AltitudeMeters?, AccuracyMeters?, SourceLabel)`
- Método: `GetCurrentLocationAsync(highAccuracy, CancellationToken)` → `Task<LocationData?>`
- Propiedad: `LastError` (string) para diagnósticos

**Implementaciones:**
- **MAUI:** `MauiGeolocationService` (usa `Microsoft.Maui.Devices.Sensors.IGeolocation` + permisos de plataforma)
- **Web WASM:** `WebGeolocationService` (usa JSInterop → `navigator.geolocation.getCurrentPosition()`)
- **Web SSR:** `SsrGeolocationService` (stub que devuelve `null` — sin acceso GPS en servidor)

**Helper JavaScript:** `ObservApp.Shared/wwwroot/geolocation.js` expone `window.observApp.getCurrentPosition(highAccuracy)` como Promise.

---

## 2. Modelos de dominio

### `ObservationSession.cs`
**Archivo:** `ObservApp.Shared/Models/ObservationSession.cs`

Representa una sesión completa de observación astronómica:

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único (auto-generado) |
| `Date` | `DateTime` | Fecha y hora de la sesión |
| `Title` | `string` | Título descriptivo |
| `Notes` | `string` | Notas libres del observador |
| `Latitude` | `double?` | Latitud GPS |
| `Longitude` | `double?` | Longitud GPS |
| `LocationName` | `string` | Nombre del lugar |
| `Seeing` | `SeeingCondition` | Calidad del seeing (1–5) |
| `Transparency` | `TransparencyCondition` | Transparencia atmosférica (1–5) |
| `Targets` | `List<ObservationTarget>` | Objetos observados |

### `ObservationTarget.cs`
**Archivo:** `ObservApp.Shared/Models/ObservationTarget.cs`

Representa un objeto astronómico observado dentro de una sesión:

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nombre del objeto (ej. "M42") |
| `Type` | `TargetType` | Tipo de objeto (planeta, DSO, cometa…) |
| `Constellation` | `string` | Constelación |
| `Eyepiece` | `string` | Ocular utilizado |
| `Magnification` | `int?` | Aumentos |
| `Notes` | `string` | Notas del objeto |
| `Rating` | `int` | Valoración 0–5 |

**Enumeraciones:**
- `TargetType`: `Planet`, `Moon`, `DeepSkyObject`, `DoubleStar`, `Asteroid`, `Comet`, `Other`
- `SeeingCondition`: `Poor(1)` ... `Excellent(5)`
- `TransparencyCondition`: `Poor(1)` ... `Excellent(5)`

---

## 3. Estado global reactivo

### `AppState.cs`
**Archivo:** `ObservApp.Shared/State/AppState.cs`

Servicio singleton que actúa como fuente de verdad reactiva para estado compartido entre componentes:

```csharp
public class AppState
{
	public string Theme { get; set; } = "dark";
	public bool IsAuthenticated { get; set; }
	public string? UserDisplayName { get; set; }

	public event Action? OnStateChanged;

	private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
```

**Uso en componentes Blazor:**
```razor
@inject AppState State
@implements IDisposable

protected override void OnInitialized()
	=> State.OnStateChanged += StateHasChanged;

public void Dispose()
	=> State.OnStateChanged -= StateHasChanged;
```

---

## 4. Patrón MVVM con CommunityToolkit.Mvvm

### Dependencias añadidas

| Proyecto | Paquete | Versión |
|----------|---------|---------|
| `ObservApp.Shared` | `CommunityToolkit.Mvvm` | 8.4.0 |
| `ObservApp.Web` | `CommunityToolkit.Mvvm` | 8.4.0 |

### `BaseViewModel.cs`
**Archivo:** `ObservApp.Shared/ViewModels/BaseViewModel.cs`

Clase base abstracta que hereda de `ObservableObject`:

| Miembro | Tipo | Descripción |
|--------|------|-------------|
| `IsBusy` | `bool` | Indica operación en curso |
| `IsNotBusy` | `bool` | Inverso calculado |
| `ErrorMessage` | `string` | Mensaje de error |
| `ClearError()` | método | Limpia el mensaje |

### `HistorialViewModel.cs`
**Archivo:** `ObservApp.Shared/ViewModels/HistorialViewModel.cs`

ViewModel para la pantalla de historial de sesiones:

```csharp
public partial class HistorialViewModel : BaseViewModel
{
	[ObservableProperty]
	private ObservationSession? selectedSession;

	[ObservableProperty]
	private ObservableCollection<ObservationSession> sessions = new();

	[RelayCommand]
	public async Task LoadSessions(CancellationToken ct)
	{
		IsBusy = true;
		try
		{
			var items = await _sessionService.GetAllAsync(ct);
			Sessions = new ObservableCollection<ObservationSession>(items);
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	public void SelectSession(ObservationSession session)
		=> SelectedSession = session;
}
```

### `ConfiguracionViewModel.cs`
**Archivo:** `ObservApp.Shared/ViewModels/ConfiguracionViewModel.cs`

ViewModel para la pantalla de configuración:

```csharp
public partial class ConfiguracionViewModel : BaseViewModel
{
	[ObservableProperty]
	private string selectedTheme;

	[ObservableProperty]
	private string selectedLanguage;

	[ObservableProperty]
	private IReadOnlyList<LanguageOption> supportedLanguages;

	[RelayCommand]
	public async Task ApplyTheme()
		=> await _settingsService.SetTheme(SelectedTheme);

	[RelayCommand]
	public async Task ApplyLanguage()
	{
		await _settingsService.SetLanguage(SelectedLanguage);
		_localizationService.SetLanguage(SelectedLanguage);
	}
}
```

---

## 5. Calculadora Sol y Luna

### `CalculadoraSolLuna.razor`
**Archivo:** `ObservApp.Shared/Pages/CalculadoraSolLuna.razor`

Nueva calculadora astronómica en `/calculadoras/soluna` que proporciona cálculos offline de alta precisión:

**Paneles disponibles:**

| Panel | Contenido |
|---|---|
| ☀️ Sol | Salida · Tránsito · Puesta · Duración del día · Crepúsculos civil/náutico/astronómico · Azimuts · Altitud máxima · Declinación · AR · Ecuación del tiempo · Distancia UA · Trayectoria horaria |
| 🌙 Luna | Salida · Tránsito · Puesta · Azimuts · Altitud máxima · Distancia · Diámetro angular · Iluminación · Fase (nombre + emoji) · Edad lunar · Próximas luna nueva/llena · Trayectoria horaria |
| 🗓️ Eventos | Equinoccios · Solsticios · Listado anual de fases lunares |

**Motor de cálculo:** `CosineKitty.AstronomyEngine` proporciona algoritmos de alta precisión.

---

## 6. Localización dinámica en Blazor WASM

### Correcciones aplicadas

**Problema:** El cambio de idioma en `/configuracion` y la recarga del menú hamburguesa fallaban silenciosamente en WASM.

**Raíz del problema:** Blazor WASM por defecto descarga únicamente datos ICU de la cultura de arranque. Al intentar cambiar `CultureInfo.DefaultThreadCurrentUICulture` en runtime, no disponía de los datos necesarios.

**Fix en `ObservApp.Web.Client.csproj`:**
```xml
<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
```

Esto incluye en el bundle el conjunto completo de datos ICU (~1 MB adicional), permitiendo cambios de cultura dinámicos entre las 6 culturas soportadas.

---

## 7. Router multi-ensamblado

### `Routes.razor`
**Archivo:** `ObservApp.Shared/Routes.razor`

Añadido parámetro `AdditionalAssemblies` para soportar páginas `@page` en proyectos hosts:

```razor
<Router AppAssembly="@typeof(Routes).Assembly"
		AdditionalAssemblies="@_additionalAssemblies">
	...
</Router>

@code {
	[Parameter]
	public IEnumerable<System.Reflection.Assembly>? AdditionalAssemblies { get; set; }

	private IEnumerable<System.Reflection.Assembly> _additionalAssemblies =>
		AdditionalAssemblies ?? [];
}
```

Si en el futuro se añaden páginas en `ObservApp` o `ObservApp.Web`, pasar el ensamblado del host:
```razor
<Routes AdditionalAssemblies="@(new[] { GetType().Assembly })" />
```

---

## 8. Cierre limpio en Windows

### `App.xaml.cs`
**Archivo:** `ObservApp/App.xaml.cs`

**Problema:** El cierre forzado con `Environment.Exit(0)` mataba el proceso sin liberar recursos (destructores no ejecutados, `Dispose` ignorado), causando potencial corrupción de datos si había escrituras en curso.

**Solución:**
```csharp
private void OnWindowDestroying(object? sender, EventArgs e)
{
	try
	{
#if WINDOWS
		Microsoft.UI.Xaml.Application.Current?.Exit();  // ✅ Cierre gestionado
#endif
	}
	catch (Exception ex)
	{
		Debug.WriteLine($"[App.OnWindowDestroying] {ex.Message}");
	}
}
```

`Microsoft.UI.Xaml.Application.Exit()` envía `WM_CLOSE` permitiendo que el runtime de .NET libere recursos correctamente.

---

## 9. Registro de servicios en los hosts

### `ObservApp/MauiProgram.cs`
```csharp
builder.Services.AddSingleton<LocalizationService>();
builder.Services.AddSingleton<ILocalizationService>(
	sp => sp.GetRequiredService<LocalizationService>());
builder.Services.AddSingleton<ISettingsService, MauiSettingsService>();
builder.Services.AddSingleton<IGeolocationService, MauiGeolocationService>();
builder.Services.AddSingleton<AppState>();
builder.Services.AddTransient<HistorialViewModel>();
builder.Services.AddTransient<ConfiguracionViewModel>();
```

**Nota sobre `LocalizationService`:** Se registra una vez como Singleton concreto y luego se expone como `ILocalizationService` usando la misma instancia. Esto permite que el código MAUI (que necesita el tipo concreto) y los componentes Blazor (que conocen la interfaz) reciban el mismo objeto.

**Nota sobre ViewModels:** Se registran como `Transient` porque cada página crea su propia instancia. Usar `Singleton` causaría que el estado persista entre navegaciones, mostrando datos obsoletos.

### `ObservApp.Web/Program.cs` y `ObservApp.Web.Client/Program.cs`
Mismo conjunto de registros que en MAUI, usando las implementaciones Web (`WebLocalizationService`, `WebSettingsService`, `WebGeolocationService`).

---

## 10. Usings globales actualizados

### `ObservApp.Shared/_Imports.razor`
```razor
@using ObservApp.Shared.Models
@using ObservApp.Shared.Services
@using ObservApp.Shared.State
@using ObservApp.Shared.ViewModels
```

### `ObservApp.Web.Client/_Imports.razor`
Mismo conjunto de usings para que los componentes tengan acceso a modelos, servicios, estado y ViewModels sin declaraciones repetidas.

---

## 11. Localización: 6 idiomas completamente soportados

Se completaron todas las traducciones para los nuevos módulos (Eclipse Timer, Calculadora Sol y Luna, Ubicaciones favoritas):

| Código | Idioma | Archivo | Estado |
|---|---|---|---|
| `es` | Español | `App.es.resx` | ✅ Completo |
| `en` | English | `App.en.resx` | ✅ Completo (referencia) |
| `fr` | Français | `App.fr.resx` | ✅ Completo |
| `de` | Deutsch | `App.de.resx` | ✅ Completo |
| `it` | Italiano | `App.it.resx` | ✅ Completo |
| `ar` | العربية | `App.ar.resx` | ✅ Completo |

---

## Impacto en desarrollo futuro

### ✅ Habilitado

- Agregar nuevas páginas en `ObservApp` o `ObservApp.Web` sin tocar `ObservApp.Shared`
- Implementar servicios específicos de plataforma sin cambiar interfaces públicas
- Crear nuevos ViewModels reutilizables en todas las plataformas
- Agregar nuevos idiomas únicamente editando `App.*.resx`
- Cambiar la persistencia de Web de `localStorage` a una API backend sin tocar componentes

### ⚠️ Próximos pasos

| Prioridad | Tarea |
|-----------|-------|
| 🔴 Alta | Implementar `SupabaseService` para persistencia en la nube |
| 🔴 Alta | Implementar `AuthService` para autenticación con Supabase |
| 🟡 Media | Migrar `WebSettingsService` a API backend en lugar de localStorage |
| 🟡 Media | Implementar `AstronomyService` (wrapping de `AstronomyEngine`) |
| 🟢 Baja | Crear páginas para Mapa, Planificación, Efemérides en `ObservApp.Shared/Pages/` |
| 🟢 Baja | Agregar pruebas unitarias para ViewModels y servicios |

---

## Archivos modificados / creados

### Creados
- ✨ `ObservApp.Shared/Models/ObservationSession.cs`
- ✨ `ObservApp.Shared/Models/ObservationTarget.cs`
- ✨ `ObservApp.Shared/Services/ILocalizationService.cs`
- ✨ `ObservApp.Shared/Services/ISettingsService.cs`
- ✨ `ObservApp.Shared/Services/IGeolocationService.cs`
- ✨ `ObservApp.Shared/State/AppState.cs`
- ✨ `ObservApp.Shared/ViewModels/BaseViewModel.cs`
- ✨ `ObservApp.Shared/ViewModels/HistorialViewModel.cs`
- ✨ `ObservApp.Shared/ViewModels/ConfiguracionViewModel.cs`
- ✨ `ObservApp.Shared/Pages/CalculadoraSolLuna.razor`
- ✨ `ObservApp.Shared/Pages/CalculadoraSolLuna.razor.css`
- ✨ `ObservApp.Shared/wwwroot/geolocation.js`
- ✨ `ObservApp/Services/MauiSettingsService.cs`
- ✨ `ObservApp/Services/MauiGeolocationService.cs`
- ✨ `ObservApp.Web.Client/Services/WebLocalizationService.cs`
- ✨ `ObservApp.Web.Client/Services/WebSettingsService.cs`
- ✨ `ObservApp.Web.Client/Services/WebGeolocationService.cs`
- ✨ `ObservApp.Web/Services/SsrGeolocationService.cs`

### Modificados
- 📝 `ObservApp.Shared/_Imports.razor` — usings globales nuevos
- 📝 `ObservApp.Shared/Routes.razor` — parámetro `AdditionalAssemblies`
- 📝 `ObservApp/Services/LocalizationService.cs` — ahora implementa `ILocalizationService`
- 📝 `ObservApp/App.xaml.cs` — eliminado `Environment.Exit(0)`, cierre limpio con `Microsoft.UI.Xaml.Application.Exit()`
- 📝 `ObservApp/MauiProgram.cs` — registro completo de servicios e interfaces
- 📝 `ObservApp.Web/Program.cs` — registro de servicios Web, añadido `CommunityToolkit.Mvvm`
- 📝 `ObservApp.Web/ObservApp.Web.csproj` — dependencia `CommunityToolkit.Mvvm 8.4.0`
- 📝 `ObservApp.Web.Client/Program.cs` — configuración completa de servicios WASM
- 📝 `ObservApp.Web.Client/_Imports.razor` — usings globales nuevos
- 📝 `ObservApp.Web.Client/ObservApp.Web.Client.csproj` — `BlazorWebAssemblyLoadAllGlobalizationData = true`

---

## Testing

Para verificar los cambios:

1. **Cambio de idioma en Web:** Ir a `/configuracion`, cambiar idioma, verificar que el menú y componentes actualizan
2. **Geolocalización en MAUI:** Solicitar ubicación, verificar que muestra coordenadas correctas
3. **Geolocalización en Web:** Abrir DevTools, permitir geolocalización, verificar que Web obtiene coordenadas
4. **Calculadora Sol y Luna:** Abrir `/calculadoras/soluna`, introducir fecha/ubicación, verificar cálculos
5. **ViewModels:** Inyectar en página Blazor, ejecutar commands, verificar que `IsBusy` y `ErrorMessage` actualizan

---

## Breaking Changes

- Ninguno para usuarios finales.
- Para desarrolladores: cambios en registro de servicios (ahora requieren interfaces), pero esto es una mejora de diseño.

---

## Conclusión

Esta refactorización establece los cimientos para un proyecto escalable y mantenible. La abstracción de servicios, el estado reactivo y los ViewModels permiten agregar nuevas funcionalidades sin acoplarse a detalles de plataforma. Los próximos pasos se centran en persistencia (Supabase) y autenticación.
