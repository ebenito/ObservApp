# Mejoras técnicas priorizadas (Copilot)

> **Fecha:** 2026-09-25  
> **Objetivo:** aplicar quick wins de alto impacto y bajo esfuerzo sobre rendimiento, robustez y mantenibilidad.

## Contexto
Este documento registra mejoras incrementales acordadas para ObservApp (.NET MAUI Blazor Hybrid + Blazor WebAssembly), priorizadas por balance entre sencillez de implementación e impacto.

## Aclaración de análisis previo
- La observación sobre `nasa`, `nasaimg` y `nasaes` no depende de compartir dominio (`nasa.gov`), sino de la **unicidad del identificador lógico** usado en trazabilidad interna (`source.Id`) para errores/diagnóstico.
- Las rutas y descripciones pueden ser distintas aunque el dominio sea el mismo.

## Iteración 1
Estado: completada.

### Alcance ejecutado
1. Ajustes de DI y trazabilidad de fuentes RSS en MAUI startup.
2. Reducción de exposición de configuración en startup WASM.
3. Mejora de patrón asíncrono en `MainLayout`.
4. Consolidación de `Dispose` en calculadora de eclipses.
5. Reducción de renders innecesarios en Home.

### Registro de cambios
- `ObservApp/MauiProgram.cs`
  - `IArticleService` ahora obtiene `HttpClient` mediante `IHttpClientFactory.CreateClient("default")`.
  - IDs lógicos RSS normalizados para trazabilidad: `nasaes` y `nasaimg` quedan únicos.
- `ObservApp.Web.Client/Program.cs`
  - Eliminado volcado del JSON completo de configuración en consola.
  - Se mantiene traza informativa sin datos sensibles (`configuración cargada desde ...`).
- `ObservApp.Shared/Layout/MainLayout.razor`
  - Sustituido `async void` en `OnLanguageChanged` por ejecución controlada vía `InvokeAsync`.
  - `OnInitializedAsync` simplificado a `OnInitialized` (no había espera real).
- `ObservApp.Shared/Pages/CalculadoraTiemposEclipse.razor`
  - Eliminada duplicidad de `Dispose`.
  - Cleanup unificado: desuscripción de `LocationChanged` + liberación de `_timer` y `_simTimer`.
- `ObservApp.Shared/Pages/Home.razor`
  - `Calcular()` ya no fuerza `StateHasChanged()` dos veces.
  - Render queda gestionado desde los puntos de llamada externos.

### Validación
- Compilación de solución: **correcta** (`ObservApp.slnx`).

## Iteración 2
Estado: completada.

### Alcance ejecutado
1. Eliminación de `eval` en `DsoList` para la lógica de lightbox.
2. Eliminación de `eval` en `GestionUbicaciones` para comprobar estado de mapa Leaflet.
3. Asegurar carga de `efemerides-lightbox.js` en MAUI y WASM.

### Registro de cambios
- `ObservApp.Shared/Pages/DsoList.razor`
  - Eliminado `OnAfterRenderAsync` que inyectaba JavaScript dinámico vía `eval`.
  - El componente usa exclusivamente `showDsoLightbox/closeDsoLightbox` desde script estático.
- `ObservApp.Shared/wwwroot/leaflet-map.js`
  - Añadida función `window.observApp.isMapInitialized(containerId)` para validación explícita del mapa.
- `ObservApp.Shared/Pages/GestionUbicaciones.razor`
  - Sustituida la comprobación por `eval` por `window.observApp.isMapInitialized`.
- `ObservApp/wwwroot/index.html`
  - Añadida referencia a `_content/ObservApp.Shared/efemerides-lightbox.js`.
- `ObservApp.Web.Client/wwwroot/index.html`
  - Añadida referencia a `_content/ObservApp.Shared/efemerides-lightbox.js`.

### Validación
- Compilación de solución: **correcta** (`ObservApp.slnx`).

### Próxima iteración sugerida
1. Mover catálogo de fuentes RSS a configuración compartida para eliminar duplicación MAUI/WASM.
2. Revisar inyecciones directas de `IJSRuntime`/`HttpClient` en páginas Shared para migrarlas a interfaces de servicio.

## Detalles técnicos de la Iteración 2
Estado: completada (documentación ampliada).

Esta sección amplía el detalle técnico de la Iteración 2: cambios exactos por archivo, motivación, verificación funcional y recomendaciones de continuidad.

### Cambios por archivo
- `ObservApp.Shared/Pages/DsoList.razor`
  - Se eliminó la inyección dinámica de JavaScript mediante `eval` en `OnAfterRenderAsync`.
  - El comportamiento del lightbox queda delegado al script estático compartido, usando funciones explícitas.
  - Impacto: menor superficie de riesgo y lógica JS más trazable/mantenible.

- `ObservApp.Shared/wwwroot/leaflet-map.js`
  - Se añadió `window.observApp.isMapInitialized(containerId)`.
  - Esta función expone una comprobación explícita de estado del mapa para consumo vía JSInterop.
  - Impacto: sustitución de verificaciones dinámicas no seguras por API clara y reutilizable.

- `ObservApp.Shared/Pages/GestionUbicaciones.razor`
  - Se reemplazó la comprobación de estado con `eval` por llamada a `window.observApp.isMapInitialized`.
  - Impacto: mejora de seguridad (CSP/XSS) y reducción de acoplamiento a código JS embebido.

- `ObservApp/wwwroot/index.html`
  - Se añadió la referencia a `_content/ObservApp.Shared/efemerides-lightbox.js`.
  - Impacto: disponibilidad garantizada de funciones de lightbox en host MAUI.

- `ObservApp.Web.Client/wwwroot/index.html`
  - Se añadió la referencia a `_content/ObservApp.Shared/efemerides-lightbox.js`.
  - Impacto: disponibilidad garantizada de funciones de lightbox en host WASM.

### Motivación técnica
1. Eliminar `eval` para reducir riesgos de seguridad y facilitar cumplimiento de políticas CSP.
2. Centralizar comportamiento JS en archivos estáticos versionables.
3. Homogeneizar la carga de scripts compartidos en ambos hosts (MAUI/WASM).

### Verificación recomendada
- Compilar solución completa (`ObservApp.slnx`): debe finalizar sin errores.
- Validar `DsoList`:
  - Abrir miniatura DSO.
  - Confirmar apertura/cierre de lightbox sin errores de consola.
- Validar `GestionUbicaciones`:
  - Inicializar mapa y actualizar ubicación.
  - Confirmar ausencia de excepciones JSInterop y comportamiento consistente.
- Validar carga de script:
  - Revisar consola/red para asegurar que `_content/ObservApp.Shared/efemerides-lightbox.js` responde sin 404.

### Notas operativas y de mantenimiento
- Evitar nuevos usos de `IJSRuntime.InvokeAsync("eval", ...)` en componentes Shared.
- Priorizar funciones JS con contrato explícito dentro de `window.observApp`.
- Mantener equivalencia funcional entre hosts (MAUI WebView y WASM) al introducir scripts compartidos.

### Pasos siguientes propuestos
1. Mover catálogo de fuentes RSS a configuración compartida para eliminar duplicación MAUI/WASM.
2. Revisar inyecciones directas de `IJSRuntime`/`HttpClient` en páginas Shared para migrarlas a interfaces de servicio.

## Iteración 3
Estado: completada.

### Alcance ejecutado
1. Centralización del catálogo RSS en `ObservApp.Shared`.
2. Eliminación de duplicación de fuentes RSS en arranque MAUI y WASM.

### Registro de cambios
- `ObservApp.Shared/Services/RssCatalog.cs`
  - Nuevo catálogo compartido con `RssCatalogEntry(RssSource, LanguageCode)`.
  - Se consolidan IDs, nombres, URLs e idioma de cada fuente en un único punto.
- `ObservApp/MauiProgram.cs`
  - `IArticleService` ahora crea `RssFeedArticleProvider` desde `RssCatalog.Sources`.
  - Eliminado bloque local de definición manual de feeds.
- `ObservApp.Web.Client/Program.cs`
  - `IArticleService` ahora crea `RssFeedArticleProvider` desde `RssCatalog.Sources`.
  - Eliminado bloque local de definición manual de feeds.

### Impacto
- Coherencia funcional MAUI/WASM al compartir exactamente el mismo catálogo.
- Menor coste de mantenimiento al evitar divergencia entre hosts.
- Mejor trazabilidad de cambios en fuentes RSS.

### Validación
- Compilación de solución: **correcta** (`ObservApp.slnx`).

### Próxima iteración sugerida
1. Introducir catálogo RSS embebido en JSON (datos fuera de código) y cargador tipado en Shared.
2. Avanzar en abstracción de `HttpClient`/`IJSRuntime` directos en páginas Shared mediante interfaces de servicio.
