# Copilot Instructions — ObservApp

> Contexto completo en `Documentacion/Claude/CLAUDE_CONTEXT.md`.
> Leer siempre antes de implementar cualquier cambio.

---

## 1. Qué es este proyecto

App de astronomía multiplataforma (.NET 10, MAUI Blazor Hybrid + Blazor WASM).  
Toda la UI vive en `ObservApp.Shared` (Razor Class Library). Los hosts MAUI (`ObservApp`) y Web (`ObservApp.Web` + `ObservApp.Web.Client`) solo aportan implementaciones de servicios dependientes de plataforma.

---

## 2. Regla cardinal

`ObservApp.Shared` **no puede referenciar** APIs nativas de MAUI ni APIs de navegador.  
Solo interfaces: `ISettingsService`, `ILocalizationService`, `IGeolocationService`, `IAuthService`, etc.

---

## 3. Convenciones en componentes Razor (ObservApp.Shared)

- Inyectar **solo interfaces**, nunca implementaciones concretas.
- Textos visibles: siempre `@L["Clave"]` con `IStringLocalizer<App>` (`@inject IStringLocalizer<App> L`).
- **Nunca usar `<form>`**. Usar divs con manejadores `@onclick`.
- No mezclar `@bind` con `@oninput` en el mismo input; usar uno u otro.
- Usar `Microsoft.AspNetCore.Components.ChangeEventArgs` completo (no `ChangeEventArgs` a secas).
- Páginas privadas: envolver contenido con `<AuthGuard>`.
- SVG dinámico: generar con `InvariantCulture` y servir como `MarkupString` pregenerado.
- Cálculo astronómico: instanciar `Observer` con altitud en **km** (no metros).
- Variables de efemerides: prefijo `Eph_` (ej: `Eph_SunAltitude`).
- Estado local de paneles de detalle: campo `private bool _showDetail` en el componente, nunca en ViewModel.

---

## 4. Convenciones de servicios

| Situación | Patrón |
|---|---|
| Nuevo servicio en Shared | Crear interfaz `IXxxService` + implementación compartida si es posible |
| Implementación MAUI | `Maui` prefix: `MauiXxxService` — registrar `AddSingleton` en `MauiProgram.cs` |
| Implementación Web WASM | `Web` prefix: `WebXxxService` — registrar en `ObservApp.Web.Client/Program.cs` |
| Implementación SSR (stub) | `Ssr` prefix: `SsrXxxService` — no-op, registrar `AddScoped` en `ObservApp.Web/Program.cs` |
| Supabase | Solo en `AuthService` y `SupabaseService`. `AutoConnectRealtime = false`. |

---

## 5. Convenciones de localización

- Todas las cadenas visibles en `.resx`: `App.es.resx` (base), con copia en los 7 idiomas restantes.
- No hardcodear texto en español en componentes. Sin excepciones.
- Idioma persistido: MAUI → `Preferences`, Web → `localStorage` vía JSInterop.

---

## 6. Convenciones de cálculo astronómico

- Biblioteca: `CosineKitty.AstronomyEngine` (offline, sin API).
- `Observer(lat, lon, altKm)` — altitud siempre en **kilómetros**.
- Cálculos en `EfemeridesAstronomyService` y `HomeAstronomyService`, nunca en componentes Razor.
- Formateo para UI: `EfemeridesUiFormatter` y `HomeUiFormatter`.

---

## 7. Anti-patrones a evitar

- ❌ Hardcodear texto visible en español en `.razor`
- ❌ Usar `Preferences`, `SecureStorage`, `Geolocation` (MAUI) directamente en Shared
- ❌ Instanciar `HttpClient` directamente; usar `IHttpClientFactory` o el factory del proyecto
- ❌ Añadir entradas al menú lateral para activar calculadoras; cambiar solo la tarjeta en `Calculadoras.razor`
- ❌ Deducir colisiones por nombre de dominio de URL; distinguir dominio URL de identificador lógico
- ❌ `AutoConnectRealtime = true` en Supabase
- ❌ Lógica astronómica en ViewModels o componentes (va en los services de Astronomia)

---

## 8. Filtros DSO

El valor seleccionado de instrumento y calidad de cielo representa la **capacidad disponible** del usuario.  
Mostrar objetos cuyo requisito mínimo sea **igual o inferior**: `selectedRank >= itemRank`.

---

## 9. Verificación antes de implementar

1. Comprobar que el modelo o ViewModel existe realmente en el workspace.
2. La arquitectura prevista pero no implementada puede estar descrita en `Documentacion/Claude/CLAUDE_CONTEXT.md`.
3. No inventar rutas, servicios ni modelos que no existan en el árbol de archivos.
4. Ante duda, leer el archivo antes de modificarlo.

---

## 10. Estructura de proyectos (resumen rápido)

```
ObservApp/             → MAUI host (Android, Windows). Services/Maui*.cs
ObservApp.Shared/      → RCL. Pages/, Components/, Layout/, ViewModels/, Models/, Services/interfaces
ObservApp.Web/         → SSR host (solo dev). Services/Ssr*.cs. APIs: /api/client-config, /api/rss-proxy, /api/wp-proxy
ObservApp.Web.Client/  → WASM client. Services/Web*.cs, BrowserAuthSessionStore
ObservApp.Shared.Tests/→ Tests unitarios (xUnit)
```