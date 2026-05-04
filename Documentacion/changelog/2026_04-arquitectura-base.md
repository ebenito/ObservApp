# Arquitectura base — Capa de abstracción, estado global y mejoras

> **Rama:** `master`
> **Fecha:** 2026-04-30
> **Scope:** `ObservApp.Shared` · `ObservApp` · `ObservApp.Web` · `ObservApp.Web.Client`

---

## Contexto

Se aplicó una refactorización de arquitectura base sobre la solución `ObservApp` con el objetivo de:

- Desacoplar los componentes Razor de cualquier API nativa (MAUI / navegador).
- Introducir un mecanismo de estado global reactivo entre componentes no relacionados jerárquicamente.
- Corregir un bug de cierre agresivo del proceso en Windows.
- Preparar el router para soportar páginas definidas fuera de `ObservApp.Shared`.
- Reducir la redundancia de `@using` en los componentes Razor.

Los **modelos de dominio** (ObservationSession, ObservationTarget…) y el **patrón MVVM** (CommunityToolkit.Mvvm, ViewModels) se posponen intencionadamente hasta que el dominio esté mejor definido.

---

## Cambios

### 1 · Interfaces de servicios en `ObservApp.Shared`

**Problema:** `LocalizationService` usaba `Microsoft.Maui.Storage.Preferences` — una API exclusiva de MAUI — y se inyectaba directamente en los componentes Razor de `ObservApp.Shared`. Esto impedía que esos mismos componentes funcionasen en la versión web.

**Solución:** Se crearon dos interfaces en `ObservApp.Shared/Services/` que actúan como contrato único para ambas plataformas.

#### Archivos nuevos

| Archivo | Descripción |
|---------|-------------|
| `ObservApp.Shared/Services/ILocalizationService.cs` | Contrato de localización en tiempo de ejecución. Expone `SupportedLanguages`, `CurrentCulture`, `CurrentLanguageCode`, el evento `OnLanguageChanged` y el método `SetLanguage`. Incluye el record `LanguageOption(Code, Name, Flag)`. |
| `ObservApp.Shared/Services/ISettingsService.cs` | Contrato de preferencias de usuario. Métodos: `GetLanguage()`, `SetLanguage(string)`, `GetTheme()`, `SetTheme(string)`. |
| `ObservApp/Services/MauiSettingsService.cs` | Implementación MAUI de `ISettingsService` usando `Microsoft.Maui.Storage.Preferences`. Claves: `app_language`, `app_theme`. |
| `ObservApp.Web.Client/Services/WebLocalizationService.cs` | Implementación web de `ILocalizationService`. Cambia `CultureInfo.DefaultThreadCurrentCulture` directamente — correcto en WASM donde cada pestaña es un proceso aislado. |
| `ObservApp.Web.Client/Services/WebSettingsService.cs` | Implementación web de `ISettingsService` con diccionario en memoria (**stub temporal**). Ver limitación conocida más abajo. |

#### Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `ObservApp/Services/LocalizationService.cs` | Añadido `using ObservApp.Shared.Services;`. La clase ahora implementa `ILocalizationService`. El campo `SupportedLanguages` se convirtió en propiedad de instancia (requerida por la interfaz) respaldada por el campo estático privado `_supportedLanguages`. Se eliminó el `record LanguageOption` que estaba al final del archivo — ahora vive únicamente en `ILocalizationService.cs`. |

> **⚠️ Limitación conocida — `WebSettingsService`:** Las preferencias del usuario (idioma, tema) se pierden al recargar la página porque se almacenan en memoria. Debe reemplazarse con una implementación basada en `localStorage` via JSInterop antes de cualquier despliegue público.
> Seguimiento: [issue #1](https://github.com/ebenito/ObservApp/issues/1)

---

### 2 · Estado global reactivo — `AppState`

**Problema:** No existía ningún mecanismo para propagar cambios de estado (ej. cambio de tema desde Configuración) a componentes no relacionados jerárquicamente, como el Layout.

**Solución:** Se creó `ObservApp.Shared/State/AppState.cs`, un servicio Singleton que emite el evento `OnStateChanged` cada vez que cambia una propiedad.

#### Archivo nuevo

`ObservApp.Shared/State/AppState.cs`

```csharp
// Propiedades disponibles:
string Theme             // tema visual activo (defecto: "dark")
bool IsAuthenticated     // indica si hay sesión de usuario activa
string UserDisplayName   // nombre visible del usuario autenticado
```

Cada setter dispara `OnStateChanged`, lo que permite que cualquier componente suscrito se re-renderice automáticamente:

```razor
@inject AppState State
@implements IDisposable

protected override void OnInitialized()
    => State.OnStateChanged += StateHasChanged;

public void Dispose()
    => State.OnStateChanged -= StateHasChanged;
```

---

### 3 · Registro de servicios en los tres hosts

#### `ObservApp/MauiProgram.cs`

```csharp
// Añadido:
builder.Services.AddSingleton<LocalizationService>();
builder.Services.AddSingleton<ILocalizationService>(
    sp => sp.GetRequiredService<LocalizationService>());
builder.Services.AddSingleton<ISettingsService, MauiSettingsService>();
builder.Services.AddSingleton<AppState>();
```

`LocalizationService` se registra una sola vez como Singleton concreto y se expone además como `ILocalizationService` usando la misma instancia, evitando instanciar el servicio dos veces.

#### `ObservApp.Web/Program.cs`

```csharp
// Añadido:
builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocalizationService, WebLocalizationService>();
builder.Services.AddSingleton<ISettingsService, WebSettingsService>();
builder.Services.AddSingleton<AppState>();
```

#### `ObservApp.Web.Client/Program.cs`

Reemplazado el `Program.cs` vacío (solo tenía `RunAsync`) por una configuración completa equivalente a la del servidor, necesaria para que los componentes interactivos WASM dispongan de todos los servicios:

```csharp
builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocalizationService, WebLocalizationService>();
builder.Services.AddSingleton<ISettingsService, WebSettingsService>();
builder.Services.AddSingleton<AppState>();
builder.Services.AddScoped(sp => new HttpClient {
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
```

---

### 4 · Corrección del cierre de la aplicación Windows

**Problema:** `App.xaml.cs` tenía un bloque `finally` que llamaba a `Environment.Exit(0)` después de `Microsoft.UI.Xaml.Application.Current?.Exit()`. Esto forzaba la terminación inmediata del proceso del SO sin ejecutar los destructores de objetos administrados ni el `Dispose` de los servicios DI, con riesgo de corrupción de datos en escrituras en curso.

**Archivo modificado:** `ObservApp/App.xaml.cs`

```csharp
// Eliminado:
finally
{
    Environment.Exit(0);
}

// Resultado: solo queda el bloque catch con Debug.WriteLine
```

`Microsoft.UI.Xaml.Application.Current?.Exit()` es suficiente — envía `WM_CLOSE` a la ventana y deja que el runtime de .NET libere recursos correctamente.

---

### 5 · Router multi-ensamblado

**Problema:** `Routes.razor` buscaba rutas `@page` únicamente en el ensamblado de `ObservApp.Shared`. Si en el futuro se añaden páginas en un proyecto host (MAUI o Web), el Router no las encontraría y devolvería siempre la página 404.

**Archivo modificado:** `ObservApp.Shared/Routes.razor`

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

Cuando sea necesario registrar páginas del host, basta con pasarle el ensamblado al componente:

```razor
<Routes AdditionalAssemblies="@(new[] { GetType().Assembly })" />
```

---

### 6 · Usings globales actualizados

**Archivo modificado:** `ObservApp.Shared/_Imports.razor`

```razor
@using ObservApp.Shared.Services
@using ObservApp.Shared.State
```

**Archivo modificado:** `ObservApp.Web.Client/_Imports.razor`

```razor
@using ObservApp.Shared.Services
@using ObservApp.Shared.State
```

Eliminado el `@using ObservApp.Shared` comentado que no aportaba nada.

---

## Árbol de archivos resultante

```
ObservApp.Shared/
├── _Imports.razor                     ✏️ nuevos usings Services y State
├── Routes.razor                       ✏️ parámetro AdditionalAssemblies
├── Services/                          🆕
│   ├── ILocalizationService.cs        🆕
│   └── ISettingsService.cs            🆕
└── State/                             🆕
    └── AppState.cs                    🆕

ObservApp/
├── App.xaml.cs                        ✏️ eliminado Environment.Exit(0)
├── MauiProgram.cs                     ✏️ registro ILocalizationService, ISettingsService, AppState
└── Services/
    ├── LocalizationService.cs         ✏️ implementa ILocalizationService, record eliminado
    └── MauiSettingsService.cs         🆕

ObservApp.Web/
└── Program.cs                         ✏️ registro ILocalizationService, ISettingsService, AppState

ObservApp.Web.Client/
├── _Imports.razor                     ✏️ nuevos usings, limpieza
├── Program.cs                         ✏️ configuración completa de servicios
└── Services/                          🆕
    ├── WebLocalizationService.cs      🆕
    └── WebSettingsService.cs          🆕 (stub temporal — issue #1)
```

---

## Trabajo pendiente relacionado

| Prioridad | Tarea | Referencia |
|-----------|-------|------------|
| 🔴 Alta | Reemplazar `WebSettingsService` con implementación `localStorage` via JSInterop | [issue #1](https://github.com/ebenito/ObservApp/issues/1) |
| 🟢 Baja | Pasar `GetType().Assembly` del host a `<Routes>` cuando se añadan páginas en el host | `ObservApp.Shared/Routes.razor` — parámetro `AdditionalAssemblies` |
