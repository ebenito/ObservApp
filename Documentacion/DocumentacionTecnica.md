# ObservApp — Documentación técnica

> **Aplicación de astronomía** construida con **.NET MAUI Blazor Hybrid** (.NET 10),
> que comparte la interfaz de usuario con un proyecto **Blazor WebAssembly** (`ObservApp.Web`).
> Permite registrar sesiones de observación, consultar efemérides y planificar salidas astronómicas.

---

## Tabla de contenidos

1. [Estructura de la solución](#1-estructura-de-la-solución)
2. [Arquitectura general](#2-arquitectura-general)
3. [Refactorización y mejoras aplicadas](#3-refactorización-y-mejoras-aplicadas)
   - [3.1 Capa de abstracción de servicios](#31-capa-de-abstracción-de-servicios)
   - [3.2 Modelos de dominio](#32-modelos-de-dominio)
   - [3.3 Estado global reactivo](#33-estado-global-reactivo)
   - [3.4 Patrón MVVM con CommunityToolkit.Mvvm](#34-patrón-mvvm-con-communitytoolkitmvvm)
   - [3.5 Implementaciones de servicios por plataforma](#35-implementaciones-de-servicios-por-plataforma)
   - [3.6 Registro de servicios en los hosts](#36-registro-de-servicios-en-los-hosts)
   - [3.7 Corrección del cierre de la aplicación Windows](#37-corrección-del-cierre-de-la-aplicación-windows)
   - [3.8 Router multi-ensamblado](#38-router-multi-ensamblado)
   - [3.9 Usings globales actualizados](#39-usings-globales-actualizados)
4. [Árbol de archivos resultante](#4-árbol-de-archivos-resultante)
5. [Dependencias NuGet añadidas](#5-dependencias-nuget-añadidas)
6. [Decisiones de diseño](#6-decisiones-de-diseño)
7. [Trabajo pendiente (TODO)](#7-trabajo-pendiente-todo)

---

## 1. Estructura de la solución

```
ObservApp.sln
│
├── ObservApp/                   # Host MAUI (Android, Windows)
├── ObservApp.Shared/            # Razor Class Library compartida (UI + lógica)
├── ObservApp.Web/               # Host Blazor Web (servidor ASP.NET Core)
└── ObservApp.Web.Client/        # Proyecto Blazor WebAssembly (cliente WASM)
```

La `ObservApp.Shared` es el núcleo de la aplicación: contiene todas las páginas Razor,
componentes, modelos, interfaces, ViewModels y estado global. Los proyectos host
(`ObservApp`, `ObservApp.Web`, `ObservApp.Web.Client`) la referencian y aportan
únicamente las implementaciones que dependen de la plataforma.

---

## 2. Arquitectura general

```
┌──────────────────────────────────────────────────────────────┐
│                      ObservApp.Shared                        │
│                                                              │
│  Pages/  Components/  Layout/  ViewModels/  Models/  State/  │
│  Services/ (interfaces)                                      │
└───────────────┬──────────────────────────┬───────────────────┘
                │                          │
    ┌───────────▼──────────┐  ┌────────────▼──────────────────┐
    │     ObservApp        │  │  ObservApp.Web.Client (WASM)  │
    │     (MAUI host)      │  │  + ObservApp.Web  (SSR host)  │
    │                      │  │                               │
    │  LocalizationService │  │  WebLocalizationService       │
    │  MauiSettingsService │  │  WebSettingsService           │
    │  MauiProgram.cs      │  │  Program.cs (×2)              │
    └──────────────────────┘  └───────────────────────────────┘
```

**Principio clave:** `ObservApp.Shared` solo depende de abstracciones
(`ILocalizationService`, `ISettingsService`). Ningún componente Razor toca APIs
nativas de MAUI o APIs de navegador directamente.

---

## 3. Refactorización y mejoras aplicadas

### 3.1 Capa de abstracción de servicios

**Problema detectado:** `LocalizationService` (que usa `Preferences`, una API nativa
de MAUI) se registraba directamente en el contenedor de servicios. Los componentes
de `ObservApp.Shared` no podían funcionar sin MAUI, impidiendo la reutilización en Web.

**Solución:** Se crearon dos interfaces en `ObservApp.Shared/Services/`:

#### `ILocalizationService.cs`
```
ObservApp.Shared/Services/ILocalizationService.cs
```
- Define el contrato para cambiar el idioma de la aplicación en tiempo de ejecución.
- Expone `SupportedLanguages`, `CurrentCulture`, `CurrentLanguageCode` y el evento
  `OnLanguageChanged`.
- Incluye el record `LanguageOption(Code, Name, Flag)`, que anteriormente estaba
  definido dentro del proyecto MAUI (se eliminó la definición duplicada de
  `LocalizationService.cs`).

#### `ISettingsService.cs`
```
ObservApp.Shared/Services/ISettingsService.cs
```
- Define el contrato para leer y escribir las preferencias del usuario: idioma y tema.
- Métodos: `GetLanguage()`, `SetLanguage(string)`, `GetTheme()`, `SetTheme(string)`.

---

### 3.2 Modelos de dominio

**Problema detectado:** La solución carecía de una carpeta `Models/` con entidades
que representen el dominio de la aplicación. Toda la lógica futura dependería de
tipos primitivos o tipos anónimos.

**Solución:** Se crearon los siguientes modelos en `ObservApp.Shared/Models/`:

#### `ObservationSession.cs`
Representa una sesión de observación astronómica completa:

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único (auto-generado) |
| `Date` | `DateTime` | Fecha y hora de la sesión |
| `Title` | `string` | Título descriptivo |
| `Notes` | `string` | Notas libres del observador |
| `Latitude` | `double?` | Latitud GPS del lugar |
| `Longitude` | `double?` | Longitud GPS del lugar |
| `LocationName` | `string` | Nombre del lugar de observación |
| `Seeing` | `SeeingCondition` | Calidad del seeing (1–5) |
| `Transparency` | `TransparencyCondition` | Transparencia atmosférica (1–5) |
| `Targets` | `List<ObservationTarget>` | Objetos observados en la sesión |

#### `ObservationTarget.cs`
Representa un objeto astronómico observado dentro de una sesión:

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nombre del objeto (ej. "M42") |
| `Type` | `TargetType` | Tipo de objeto (planeta, DSO, cometa…) |
| `Constellation` | `string` | Constelación |
| `Eyepiece` | `string` | Ocular utilizado |
| `Magnification` | `int?` | Aumentos usados |
| `Notes` | `string` | Notas del objeto |
| `Rating` | `int` | Valoración del 0 al 5 |

**Enumeraciones definidas en el mismo archivo:**
- `TargetType`: `Planet`, `Moon`, `DeepSkyObject`, `DoubleStar`, `Asteroid`, `Comet`, `Other`
- `SeeingCondition`: `Poor(1)` … `Excellent(5)`
- `TransparencyCondition`: `Poor(1)` … `Excellent(5)`

---

### 3.3 Estado global reactivo

**Problema detectado:** No existía ningún mecanismo para compartir estado entre
componentes Blazor no relacionados jerárquicamente (por ejemplo, cambiar el tema
desde Configuración y que el Layout lo refleje sin recargar la página).

**Solución:** Se creó `ObservApp.Shared/State/AppState.cs`, un servicio Singleton
que actúa como fuente de verdad reactiva:

```csharp
// Uso en un componente Blazor:
@inject AppState State
@implements IDisposable

protected override void OnInitialized()
    => State.OnStateChanged += StateHasChanged;

public void Dispose()
    => State.OnStateChanged -= StateHasChanged;
```

**Propiedades actuales:**
- `Theme` (string, defecto `"dark"`) — tema visual activo
- `IsAuthenticated` (bool) — indica si hay sesión de usuario activa
- `UserDisplayName` (string) — nombre visible del usuario autenticado

Cada propiedad notifica el cambio mediante el evento `OnStateChanged`, lo que
permite que cualquier componente suscrito se re-renderice automáticamente.

---

### 3.4 Patrón MVVM con CommunityToolkit.Mvvm

> **Nota de diseño:** En Blazor Hybrid el patrón MVVM añade fricción sin el beneficio
> del binding bidireccional automático que ofrece en WPF o MAUI XAML. El re-render
> en Blazor es siempre explícito (`StateHasChanged`), por lo que se recomienda
> **aplicar ViewModels solo en páginas con lógica compleja** (Historial, Planificación,
> Efemérides) y no forzarlos en páginas simples donde servicios inyectados directamente
> son suficientes y más legibles.

**Problema detectado:** Las páginas Razor mezclaban lógica de negocio directamente
en el bloque `@code { }`, dificultando las pruebas unitarias y el mantenimiento.

**Solución:** Se añadió el paquete `CommunityToolkit.Mvvm 8.4.0` a
`ObservApp.Shared` y se creó una jerarquía de ViewModels en
`ObservApp.Shared/ViewModels/`:

#### `BaseViewModel.cs`
Clase base abstracta que hereda de `ObservableObject`. Proporciona:
- `IsBusy` (`bool`) — indica operación en curso; notifica también `IsNotBusy`
- `IsNotBusy` (`bool`) — inverso calculado de `IsBusy`
- `ErrorMessage` (`string`) — mensaje de error para mostrar en la UI
- `ClearError()` — limpia el mensaje de error

#### `HistorialViewModel.cs`
ViewModel para la pantalla de historial de sesiones:
- `Sessions` — lista de `ObservationSession` cargadas
- `SelectedSession` — sesión seleccionada actualmente
- `LoadSessionsCommand` — carga asíncrona con gestión de `IsBusy` y `ErrorMessage`
- `SelectSessionCommand` — establece la sesión activa
- `ClearSelectionCommand` — limpia la selección

#### `ConfiguracionViewModel.cs`
ViewModel para la pantalla de configuración:
- `SelectedTheme` / `SelectedLanguage` — valores actuales leídos desde `ISettingsService`
- `SupportedLanguages` — lista de idiomas disponibles desde `ILocalizationService`
- `ApplyThemeCommand` — persiste el tema seleccionado
- `ApplyLanguageCommand` — persiste el idioma y notifica a `ILocalizationService`

**Cómo inyectar un ViewModel en una página Blazor:**
```razor
@inject HistorialViewModel VM

protected override async Task OnInitializedAsync()
    => await VM.LoadSessionsCommand.ExecuteAsync(null);
```

---

### 3.5 Implementaciones de servicios por plataforma

#### MAUI — `LocalizationService.cs` (modificado)
```
ObservApp/Services/LocalizationService.cs
```
- Se añadió `using ObservApp.Shared.Services;`
- La clase ahora implementa `ILocalizationService`
- Se eliminó el `record LanguageOption` que estaba al final del archivo (ahora
  vive en `ILocalizationService.cs` dentro de Shared, evitando duplicados)
- El resto de la lógica (uso de `Preferences`, detección del idioma del sistema)
  no fue modificado

#### MAUI — `MauiSettingsService.cs` (nuevo)
```
ObservApp/Services/MauiSettingsService.cs
```
Implementa `ISettingsService` usando `Microsoft.Maui.Storage.Preferences`:
- Clave `"app_language"` → idioma persistido entre sesiones
- Clave `"app_theme"` → tema persistido entre sesiones

#### Web — `WebLocalizationService.cs` (nuevo)
```
ObservApp.Web.Client/Services/WebLocalizationService.cs
```
Implementa `ILocalizationService` sin dependencias nativas. Cambia
`CultureInfo.DefaultThreadCurrentCulture` y `DefaultThreadCurrentUICulture`
directamente, lo que es correcto en contexto WASM donde cada pestaña del navegador
es un proceso aislado.

#### Web — `WebSettingsService.cs` (nuevo)
```
ObservApp.Web.Client/Services/WebSettingsService.cs
```
Implementa `ISettingsService` con un diccionario en memoria. Los valores se
pierden al recargar la página.

> ⚠️ **Limitación conocida:** Un usuario de la versión web que recargue la página
> perderá toda su configuración (idioma, tema). Para producción esta implementación
> **no es aceptable**; debe reemplazarse por `localStorage` via JSInterop antes
> de cualquier despliegue público.
>
> **TODO:** Reemplazar con una implementación basada en `JSInterop` →
> `window.localStorage` para que las preferencias persistan entre sesiones web.

---

### 3.6 Registro de servicios en los hosts

#### `ObservApp/MauiProgram.cs` (modificado)

Se añadieron los `using` necesarios y se registraron todos los servicios:

```csharp
// Antes (incompleto):
builder.Services.AddSingleton<LocalizationService>();

// Después (completo):
builder.Services.AddSingleton<LocalizationService>();
builder.Services.AddSingleton<ILocalizationService>(
    sp => sp.GetRequiredService<LocalizationService>());
builder.Services.AddSingleton<ISettingsService, MauiSettingsService>();
builder.Services.AddSingleton<AppState>();
builder.Services.AddTransient<HistorialViewModel>();
builder.Services.AddTransient<ConfiguracionViewModel>();
```

`LocalizationService` se registra una sola vez como Singleton concreto y luego
se expone como `ILocalizationService` usando la misma instancia. Esto garantiza
que tanto el código MAUI (que necesita el tipo concreto para el constructor con
`Preferences`) como los componentes Blazor (que solo conocen la interfaz) reciban
el mismo objeto.

Los ViewModels se registran como `Transient` porque cada página crea su propia
instancia y los libera al destruirse.

Se eliminaron los comentarios TODO de servicios ya implementados
(`ISettingsService`, `ThemeService`) para mantener el archivo limpio.

#### `ObservApp.Web/Program.cs` (modificado)

Se añadió el mismo conjunto de registros de servicios que en MAUI, usando las
implementaciones Web. También se añadió la referencia a `CommunityToolkit.Mvvm`
en `ObservApp.Web.csproj` porque el servidor SSR también instancia los ViewModels.

#### `ObservApp.Web.Client/Program.cs` (modificado)

Se reemplazó el `Program.cs` vacío (solo tenía `RunAsync`) por una configuración
completa equivalente a la del servidor, necesaria para que los componentes WASM
tengan todos los servicios disponibles cuando se ejecutan en el cliente:

```csharp
builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocalizationService, WebLocalizationService>();
builder.Services.AddSingleton<ISettingsService, WebSettingsService>();
builder.Services.AddSingleton<AppState>();
builder.Services.AddTransient<HistorialViewModel>();
builder.Services.AddTransient<ConfiguracionViewModel>();
builder.Services.AddScoped(sp => new HttpClient {
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
```

---

### 3.7 Corrección del cierre de la aplicación Windows

**Problema detectado:** `App.xaml.cs` usaba `Environment.Exit(0)` en el bloque
`finally` de `OnWindowDestroying`. Esta llamada fuerza la terminación inmediata
del proceso del sistema operativo, sin ejecutar los destructores de objetos
administrados ni el `Dispose` de los servicios registrados en el contenedor DI.
Podía causar corrupción de datos si había escrituras en curso (ej. Supabase, SQLite).

**Código problemático (eliminado):**
```csharp
finally
{
    // Garantía de cierre si Exit() no termina el proceso
    Environment.Exit(0);  // ❌ mata el proceso sin liberar recursos
}
```

**Código corregido:**
```csharp
private void OnWindowDestroying(object? sender, EventArgs e)
{
    try
    {
        // Cierre limpio del proceso en Windows
#if WINDOWS
        Microsoft.UI.Xaml.Application.Current?.Exit();  // ✅ cierre gestionado por WinUI
#endif
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[App.OnWindowDestroying] {ex.Message}");
    }
}
```

`Microsoft.UI.Xaml.Application.Exit()` envía el mensaje `WM_CLOSE` a la ventana,
permitiendo que el runtime de .NET libere recursos correctamente antes de terminar.

---

### 3.8 Router multi-ensamblado

**Problema detectado:** `Routes.razor` usaba únicamente `AppAssembly="@typeof(Routes).Assembly"`,
lo que limitaba la búsqueda de rutas `@page` al ensamblado de `ObservApp.Shared`.
Si en el futuro se añaden páginas en el proyecto host (MAUI o Web), el Router
no las encontraría y devolvería siempre la página 404.

**Solución:** Se añadió un parámetro `AdditionalAssemblies` a `Routes.razor`:

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

**Cómo pasar el ensamblado del host** (cuando sea necesario):
```razor
<!-- En MainPage.xaml o en App.razor del host -->
<Routes AdditionalAssemblies="@(new[] { GetType().Assembly })" />
```

---

### 3.9 Usings globales actualizados

Se actualizaron los archivos `_Imports.razor` de `ObservApp.Shared` y
`ObservApp.Web.Client` para incluir los nuevos namespaces, evitando que cada
componente tenga que declararlos individualmente:

**Añadidos a `ObservApp.Shared/_Imports.razor`:**
```razor
@using ObservApp.Shared.Models
@using ObservApp.Shared.Services
@using ObservApp.Shared.State
@using ObservApp.Shared.ViewModels
```

**Añadidos a `ObservApp.Web.Client/_Imports.razor`:**
```razor
@using ObservApp.Shared.Models
@using ObservApp.Shared.Services
@using ObservApp.Shared.State
@using ObservApp.Shared.ViewModels
```

---

## 4. Árbol de archivos resultante

```
ObservApp.Shared/
├── _Imports.razor                          # ✏️ Modificado — nuevos usings
├── AssemblyReference.cs
├── Routes.razor                            # ✏️ Modificado — multi-ensamblado
├── Layout/
│   └── MainLayout.razor
├── Models/                                 # 🆕 Carpeta nueva
│   ├── ObservationSession.cs               # 🆕 Modelo de sesión de observación
│   └── ObservationTarget.cs               # 🆕 Modelo de objeto + enumeraciones
├── Pages/
│   ├── Home.razor
│   └── NotFound.razor
├── Resources/
│   └── Strings/
│       ├── App.en.resx
│       ├── App.es.resx
│       └── AppStrings.cs
├── Services/                              # 🆕 Carpeta nueva
│   ├── ILocalizationService.cs            # 🆕 Interfaz de localización
│   └── ISettingsService.cs               # 🆕 Interfaz de preferencias
├── State/                                 # 🆕 Carpeta nueva
│   └── AppState.cs                        # 🆕 Estado global reactivo
├── ViewModels/                            # 🆕 Carpeta nueva
│   ├── BaseViewModel.cs                   # 🆕 ViewModel base
│   ├── ConfiguracionViewModel.cs          # 🆕 ViewModel de configuración
│   └── HistorialViewModel.cs              # 🆕 ViewModel de historial
└── wwwroot/
    └── app.css

ObservApp/
├── App.xaml.cs                            # ✏️ Modificado — eliminado Environment.Exit(0)
├── MauiProgram.cs                         # ✏️ Modificado — registro completo de servicios
├── MainPage.xaml.cs
├── Services/
│   ├── LocalizationService.cs             # ✏️ Modificado — implementa ILocalizationService
│   └── MauiSettingsService.cs             # 🆕 Implementación MAUI de ISettingsService
└── Platforms/...

ObservApp.Web/
├── Program.cs                             # ✏️ Modificado — registro completo de servicios
└── ObservApp.Web.csproj                   # ✏️ Modificado — añadido CommunityToolkit.Mvvm

ObservApp.Web.Client/
├── _Imports.razor                         # ✏️ Modificado — nuevos usings
├── Program.cs                             # ✏️ Modificado — registro completo de servicios
└── Services/                             # 🆕 Carpeta nueva
    ├── WebLocalizationService.cs          # 🆕 Implementación Web de ILocalizationService
    └── WebSettingsService.cs              # 🆕 Implementación Web de ISettingsService
```

---

## 5. Dependencias NuGet añadidas

| Proyecto | Paquete | Versión | Motivo |
|----------|---------|---------|--------|
| `ObservApp.Shared` | `CommunityToolkit.Mvvm` | 8.4.0 | Source generators para ViewModels (`[ObservableProperty]`, `[RelayCommand]`) |
| `ObservApp.Web` | `CommunityToolkit.Mvvm` | 8.4.0 | El servidor SSR instancia los ViewModels definidos en Shared |

El paquete `CommunityToolkit.Maui` ya estaba presente en `ObservApp` desde el inicio.
`ObservApp.Web.Client` hereda `CommunityToolkit.Mvvm` transitivamente a través de
la referencia a `ObservApp.Shared`.

---

## 6. Decisiones de diseño

### ¿Por qué `LocalizationService` en MAUI y no en Shared?

`LocalizationService` usa `Microsoft.Maui.Storage.Preferences`, una API que no
existe en el runtime web. Moverla a Shared causaría un error de compilación en
`ObservApp.Web.Client`. La solución es mantenerla en el proyecto MAUI pero hacer
que implemente la interfaz `ILocalizationService` definida en Shared.

### ¿Por qué `AddTransient` para los ViewModels?

Los ViewModels se registran como `Transient` porque:
1. Cada página Blazor crea su propia instancia al montarse.
2. Blazor gestiona el ciclo de vida del componente; cuando la página se destruye,
   el ViewModel es elegible para el GC.
3. Usar `Singleton` causaría que el estado de una página (ej. `Sessions` en
   `HistorialViewModel`) persista entre navegaciones, mostrando datos obsoletos.

### ¿Por qué `AppState` como `Singleton`?

`AppState` es compartido y debe ser la misma instancia para todos los componentes.
Si fuera `Transient` o `Scoped`, cada componente tendría su propio estado y el
evento `OnStateChanged` no propagaría cambios entre componentes.

### ¿Por qué `WebSettingsService` usa un diccionario en memoria?

En WASM, `localStorage` solo es accesible mediante `JSInterop`, lo que requiere
código asíncrono y manejo del ciclo de vida del navegador. Como paso intermedio,
se implementó un almacén en memoria que cumple el contrato de `ISettingsService`.
La migración a `localStorage` es un cambio localizado en un solo archivo.

---

## 7. Trabajo pendiente (TODO)

| Prioridad | Tarea | Archivo relacionado |
|-----------|-------|---------------------|
| 🔴 Alta | Implementar `SupabaseService` para persistencia en la nube | `MauiProgram.cs` (comentario TODO) |
| 🔴 Alta | Implementar `AuthService` para autenticación con Supabase | `MauiProgram.cs` (comentario TODO) |
| 🔴 Alta | Implementar `GeolocationService` — funcionalidad central de la app | `MauiProgram.cs` (comentario TODO) |
| 🟡 Media | Reemplazar `WebSettingsService` con `localStorage` via JSInterop | `ObservApp.Web.Client/Services/WebSettingsService.cs` |
| 🟡 Media | Implementar `AstronomyService` (wrapping de `AstronomyEngine`) | `MauiProgram.cs` (comentario TODO) |
| 🟢 Baja | Pasar `GetType().Assembly` del host a `<Routes>` cuando se añadan páginas en el host | `Routes.razor` (parámetro `AdditionalAssemblies`) |
| 🟢 Baja | Crear páginas Razor para todas las rutas del menú (Mapa, Planificación, Efemérides…) | `ObservApp.Shared/Pages/` |
| 🟢 Baja | Añadir pruebas unitarias para los ViewModels | Proyecto de tests nuevo |

