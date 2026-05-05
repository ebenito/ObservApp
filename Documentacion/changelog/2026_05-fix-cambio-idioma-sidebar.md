# Fix: cambio de idioma y menú hamburguesa inoperativos en Blazor WASM

> **Rama:** `master`
> **Fecha:** 2026-05-05
> **Scope:** `ObservApp.Web.Client` · `ObservApp.Shared` · `ObservApp.Web`
> **Issue:** [#1 — WebSettingsService: reemplazar diccionario en memoria por localStorage](https://github.com/ebenito/ObservApp/issues/1)

---

## Contexto

Tras completar la implementación inicial de `WebSettingsService` con persistencia en `localStorage`
(issue #1), el flujo de cambio de idioma en `/configuracion` y el menú hamburguesa del `MainLayout`
seguían sin funcionar en la versión Blazor WASM. Se identificaron cinco bugs y mejoras independientes,
algunos encadenados, que se enmascaraban entre sí.

---

## Bugs identificados y corregidos

### Bug 1 · `BlazorWebAssemblyLoadAllGlobalizationData` ausente

**Archivo:** `ObservApp.Web.Client/ObservApp.Web.Client.csproj`

**Síntoma:**
Al seleccionar un idioma diferente al de arranque, Blazor WASM lanzaba la siguiente excepción
en consola del navegador y abortaba silenciosamente la operación:

```
ManagedError: Blazor detected a change in the application's culture that is not supported
with the current project configuration. To change culture dynamically during startup, set
<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
in the application's project file.
```

**Causa:**
Blazor WASM descarga por defecto únicamente los datos ICU de la cultura de arranque
(ICU data trimming). Al intentar cambiar `CultureInfo.DefaultThreadCurrentUICulture`
en `Program.cs` a otra cultura en runtime, el runtime no disponía de los datos necesarios.

**Fix:**
```xml
<!-- ObservApp.Web.Client.csproj -->
<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
```

Esto incluye en el bundle el conjunto completo de datos ICU, permitiendo cambios de cultura
dinámicos. El coste es un incremento de ~1 MB en la descarga inicial, asumible para una
app multiidioma con 6 culturas.

---

### Bug 2 · `location.reload` llamado sin contexto `this` desde JSInterop

**Archivo:** `ObservApp.Web.Client/Services/WebLocalizationService.cs`

**Síntoma:**
Tras seleccionar un idioma, la UI mostraba el toast "Aplicando idioma…" y luego volvía al
idioma anterior sin recargar. No se producía ningún error visible (el error se tragaba en
el fire-and-forget).

**Causa:**
```csharp
// ❌ Incorrecto — location.reload se pasa como referencia de función desvinculada
await _js.InvokeVoidAsync("location.reload");
```

JSInterop resuelve `location.reload` como una referencia de función sin su objeto `this`
nativo (`window.location`). Al ejecutarla, el navegador lanza `TypeError: Illegal invocation`
que el bloque `_ = SetLanguageAndReloadAsync(...)` (fire-and-forget) absorbía silenciosamente.

**Fix inicial** (luego refinado, ver Bug 3):
Mover `location.reload()` dentro de una función JS donde tiene el contexto correcto.

---

### Bug 3 · Bucle infinito de recargas por `applyLang` compartida

**Archivos:**
- `ObservApp.Shared/wwwroot/loading-texts.js`
- `ObservApp.Shared/Layout/MainLayout.razor`
- `ObservApp.Web.Client/Services/WebLocalizationService.cs`

**Síntoma:**
Tras el fix del Bug 2 (añadir `location.reload()` dentro de `applyLang`), la aplicación
entraba en un bucle infinito de recargas: el menú hamburguesa nunca llegaba a desplegarse
y el cambio de idioma tampoco se completaba.

**Causa:**
`MainLayout.razor` llama a `ObservApp.applyLang` en `OnAfterRenderAsync` en cada primer
render para sincronizar los atributos `lang`/`dir` del `<html>`. Al añadir `location.reload()`
dentro de `applyLang`, este ciclo se convertía en:

```
1. Blazor carga → MainLayout primer render
2. OnAfterRenderAsync → applyLang() → location.reload()
3. Blazor recarga → MainLayout primer render
4. OnAfterRenderAsync → applyLang() → location.reload()
5. … bucle infinito
```

**Fix:**
Se separaron las dos responsabilidades en funciones JS distintas:

```javascript
// Solo actualiza <html lang/dir> y localStorage — usa MainLayout en cada render
window.ObservApp.applyLang = function (code, dir) {
    document.documentElement.lang = code;
    document.documentElement.dir  = dir;
    try { localStorage.setItem("app_language", code); } catch (e) { }
};

// Igual que applyLang + recarga de página — usa exclusivamente el cambio de idioma
window.ObservApp.setLangAndReload = function (code, dir) {
    document.documentElement.lang = code;
    document.documentElement.dir  = dir;
    try { localStorage.setItem("app_language", code); } catch (e) { }
    location.reload();
};
```

`WebLocalizationService.SetLanguageAndReloadAsync` actualizado para usar `setLangAndReload`:

```csharp
await _js.InvokeVoidAsync("ObservApp.setLangAndReload",
    languageCode,
    languageCode == "ar" ? "rtl" : "ltr");
```

---

### Bug 4 · Atributo `Closed` inexistente en `SfSidebar` v33 — cierre al hacer clic fuera

**Archivo:** `ObservApp.Shared/Layout/MainLayout.razor`

**Síntoma:**
El menú hamburguesa no se cerraba al hacer clic fuera de él. IntelliSense no reconocía
el atributo `Closed="OnSidebarClosed"` añadido inicialmente en `SfSidebar`.

**Causa:**
El evento `Closed` no existe en Syncfusion Blazor v33. Se verificó inspeccionando
directamente la DLL `Syncfusion.Blazor.Navigations 33.2.3`, donde el símbolo correcto es
`CloseOnDocumentClick` (propiedad `bool`), no un `EventCallback`.

El primer intento de solución añadió un listener JS manual con `DotNetObjectReference`
y `[JSInvokable]` que resultó innecesario al descubrir la propiedad nativa.

**Fix:**
Usar la propiedad nativa del control, que gestiona el listener internamente y actualiza
`IsOpen` a través del binding `@bind-IsOpen`:

```razor
<SfSidebar @bind-IsOpen="_sidebarOpen"
           CloseOnDocumentClick="true"
           ...>
```

Se eliminó todo el código auxiliar añadido innecesariamente:
- Campo `_dotNetRef` (`DotNetObjectReference<MainLayout>`)
- Método `[JSInvokable] CloseFromOutside()`
- Método `OnSidebarClosed()`
- Funciones JS `registerSidebarClickOutside` / `unregisterSidebarClickOutside`
- Llamadas `JS.InvokeVoidAsync("ObservApp.registerSidebarClickOutside", ...)` y `unregister`

---

### Bug 5 · Conflicto de puerto en IIS Express (0x80070020)

**Archivo:** `ObservApp.Web/Properties/launchSettings.json`

**Síntoma:**
Al iniciar la versión web con IIS Express aparecía el error:

```
Failed to register URL "http://localhost:49217/" for site "ObservApp.Web" application "/".
Error description: El proceso no tiene acceso al archivo porque está siendo utilizado
por otro proceso. (0x80070020)
```

**Causa:**
Chrome tenía una conexión TCP establecida con origen en el puerto 49217
(`192.168.1.39:49217 → 34.54.194.141:443`, PID 18012). Windows no permite que IIS Express
haga `bind` en ese mismo puerto mientras otra aplicación lo usa.

**Fix:**
```json
// launchSettings.json — iisExpress
"applicationUrl": "http://localhost:49380/"   // antes: 49217
```

El puerto HTTPS (`44331`) no estaba en conflicto y se mantuvo.

---

## Mejoras adicionales

### Icono del menú de Calculadoras

**Archivo:** `ObservApp.Shared/Layout/MainLayout.razor`

Se corrigió el icono del enlace de navegación a `/calculadoras`, que usaba una cámara de
fotos (`📷`) poco representativa:

```razor
<!-- Antes -->
<span class="obs-nav-icon">📷</span>

<!-- Después -->
<span class="obs-nav-icon">🧮</span>
```

---

## Resumen de archivos modificados

| Archivo | Cambio |
|---|---|
| `ObservApp.Web.Client/ObservApp.Web.Client.csproj` | Añadida propiedad `BlazorWebAssemblyLoadAllGlobalizationData` |
| `ObservApp.Shared/wwwroot/loading-texts.js` | Separada `applyLang` (sin reload) de `setLangAndReload` (con reload); eliminadas funciones JS de click-outside |
| `ObservApp.Web.Client/Services/WebLocalizationService.cs` | Usa `setLangAndReload` en lugar de `applyLang` + `location.reload` separado |
| `ObservApp.Shared/Layout/MainLayout.razor` | `CloseOnDocumentClick="true"` en `SfSidebar`; eliminado código `DotNetObjectReference` / `[JSInvokable]`; icono nav calculadoras `📷 → 🧮` |
| `ObservApp.Web/Properties/launchSettings.json` | Puerto IIS Express cambiado de `49217` a `49380` |

---

## Comportamiento final

| Acción | Antes | Después |
|---|---|---|
| Seleccionar idioma en `/configuracion` | UI vuelve al idioma anterior, sin recarga | `localStorage` actualizado → recarga → idioma aplicado |
| Menú hamburguesa — abrir/cerrar | No se desplegaba (bucle de recargas) | Se abre y cierra correctamente |
| Menú hamburguesa — clic fuera | No se cerraba | Se cierra automáticamente |
| Persistencia de idioma entre recargas | No persistía (issue #1) | Persiste via `localStorage` |
| Soporte de 6 culturas en WASM | Solo cultura de arranque | Todas las culturas disponibles en runtime |
| Inicio con IIS Express | Error 0x80070020 (conflicto de puerto) | Arranca correctamente en puerto 49380 |

---

## Notas técnicas

- El incremento de bundle por `BlazorWebAssemblyLoadAllGlobalizationData` es el tradeoff
  estándar y documentado por Microsoft para apps Blazor WASM multiidioma.
- `applyLang` se mantiene como función pública para uso interno del layout y potenciales
  llamadas externas (ej. splash screen, index.html) que no deben provocar recarga.
- El patrón fire-and-forget (`_ = Task`) en `SetLanguage` se conserva porque
  `ISettingsService` es síncrono por diseño de interfaz; la escritura en `localStorage`
  es best-effort y no bloquea la UI.
- `CloseOnDocumentClick` de Syncfusion gestiona internamente su propio listener JS,
  por lo que no se necesita ningún código adicional en C# ni en JS para este comportamiento.
