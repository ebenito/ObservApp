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

### Próxima iteración sugerida
1. Reemplazar usos de `eval` en Shared (`DsoList` y `GestionUbicaciones`) por funciones JSInterop explícitas.
2. Mover catálogo de fuentes RSS a configuración compartida para eliminar duplicación MAUI/WASM.
