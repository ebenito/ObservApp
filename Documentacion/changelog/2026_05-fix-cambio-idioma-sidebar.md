# Fix: cambio de idioma y menú hamburguesa inoperativos en Blazor WASM

> **Rama:** `master`
> **Fecha:** 2026-05-05
> **Scope:** `ObservApp.Web.Client` · `ObservApp.Shared`
> **Issue:** [#1 — WebSettingsService: reemplazar diccionario en memoria por localStorage](https://github.com/ebenito/ObservApp/issues/1)

---

## Contexto

Tras completar la implementación inicial de `WebSettingsService` con persistencia en `localStorage`
(issue #1), el flujo de cambio de idioma en `/configuracion` y el menú hamburguesa del `MainLayout`
seguían sin funcionar en la versión Blazor WASM. Se identificaron tres bugs independientes,
encadenados, que se enmascaraban entre sí.

---

## Bugs identificados

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

## Resumen de archivos modificados

| Archivo | Cambio |
|---|---|
| `ObservApp.Web.Client/ObservApp.Web.Client.csproj` | Añadida propiedad `BlazorWebAssemblyLoadAllGlobalizationData` |
| `ObservApp.Shared/wwwroot/loading-texts.js` | Separada `applyLang` (sin reload) de `setLangAndReload` (con reload) |
| `ObservApp.Web.Client/Services/WebLocalizationService.cs` | Usa `setLangAndReload` en lugar de `applyLang` + `location.reload` separado |

---

## Comportamiento final

| Acción | Antes | Después |
|---|---|---|
| Seleccionar idioma en `/configuracion` | UI vuelve al idioma anterior, sin recarga | `localStorage` actualizado → recarga → idioma aplicado |
| Menú hamburguesa | No se desplegaba (bucle de recargas) | Se abre y cierra correctamente |
| Persistencia entre recargas | No persistía (issue #1) | Persiste via `localStorage` |
| Soporte de 6 culturas en WASM | Solo cultura de arranque | Todas las culturas disponibles en runtime |

---

## Notas técnicas

- El incremento de bundle por `BlazorWebAssemblyLoadAllGlobalizationData` es el tradeoff
  estándar y documentado por Microsoft para apps Blazor WASM multiidioma.
- `applyLang` se mantiene como función pública para uso interno del layout y potenciales
  llamadas externas (ej. splash screen, index.html) que no deben provocar recarga.
- El patrón fire-and-forget (`_ = Task`) en `SetLanguage` se conserva porque
  `ISettingsService` es síncrono por diseño de interfaz; la escritura en `localStorage`
  es best-effort y no bloquea la UI.
