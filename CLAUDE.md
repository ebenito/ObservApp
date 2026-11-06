# CLAUDE.md — ObservApp

> Punto de entrada para Claude Code y otros agentes de IA.  
> Para el contexto técnico completo leer: `Documentacion/Claude/CLAUDE_CONTEXT.md`  
> Para las reglas de codificación: `.github/copilot-instructions.md`

---

## Qué es ObservApp

Aplicación de astronomía multiplataforma (.NET 10) que comparte toda la UI en `ObservApp.Shared` (Razor Class Library) entre:
- **MAUI** → Android + Windows (`ObservApp/`)
- **Web** → Blazor WASM deployado en Azure Static Web Apps (`ObservApp.Web.Client/` + `ObservApp.Web/`)

Desarrollador único: Esteban. Idioma de trabajo: **español**. Co-desarrollo con GitHub Copilot en Visual Studio 2026.

---

## Regla de oro antes de cualquier cambio

```
ObservApp.Shared  →  solo interfaces, nunca APIs nativas
```

No inventar rutas, ViewModels ni servicios. Verificar su existencia real con las herramientas disponibles.

---

## Stack en una línea

`.NET 10` · `MAUI Blazor Hybrid` · `Blazor WASM` · `Syncfusion Blazor 35.x` · `CommunityToolkit.Mvvm` · `CosineKitty.AstronomyEngine` · `Supabase 8.x` · `Leaflet.js` · `GitVersion`

---

## Proyectos

| Proyecto | Rol |
|---|---|
| `ObservApp/` | Host MAUI. `MauiProgram.cs` registra todos los servicios nativos. |
| `ObservApp.Shared/` | **Núcleo.** Pages, Components, Layout, ViewModels, Models, Services (interfaces + impl. compartidas), i18n (8 idiomas). |
| `ObservApp.Web/` | Host SSR para desarrollo local. Stubs `Ssr*.cs`. APIs proxy en `Program.cs`. |
| `ObservApp.Web.Client/` | Cliente WASM. Servicios `Web*.cs`. Deploy en Azure Static Web Apps. |
| `api/` | Azure Functions (Node.js 22) de la web publicada: `/api/rss-proxy` y `/api/wp-proxy`, equivalentes a los de `ObservApp.Web/Program.cs`. Mantener ambos sincronizados. Tests: `npm test` en `api/`. |
| `ObservApp.Shared.Tests/` | Tests unitarios xUnit v3 (Microsoft Testing Platform, activado en `global.json`). |

---

## Convenciones clave (resumen ejecutivo)

| Qué | Cómo |
|---|---|
| Texto en componentes | `@L["Clave"]` con `@inject IStringLocalizer<App> L`; clave nueva siempre en los 8 `.resx` a la vez |
| Mensajes de error en servicios | Inyectar `IStringLocalizer<App>` y usar claves `Auth_Error_*`, `Gps_Error_*`, `Articles_Error_*`, `Rss_Error_*`, `Obs_Error_*`; con parámetros `_l["Clave", arg]`. Nunca texto literal en `LastError` |
| Formularios | `<div>` + `@onclick`, **nunca** `<form>` |
| Nuevo servicio multiplataforma | Interfaz en `Shared/Services/`, implementaciones `Maui*`, `Web*`, `Ssr*` |
| Cálculo astronómico | En servicios (`Services/Astronomia/`, `Services/Calculadoras/<Nombre>/`) con tests; `Observer` con altitud en **metros** |
| Variables efemerides | Prefijo `Eph_` |
| Calculadoras nuevas | Activar en `Calculadoras.razor` (cambiar clase CSS), **no** tocar el menú lateral |
| Páginas privadas | Envolver con `<AuthGuard>` |
| Supabase | `AutoConnectRealtime = false` siempre |

---

## Rutas de la app

| Ruta | Componente |
|---|---|
| `/` | `Home.razor` |
| `/efemerides` | `Efemerides.razor` |
| `/calculadoras` | `Calculadoras.razor` (hub) |
| `/calculadoras/soluna` | `CalculadoraSolLuna.razor` |
| `/calculadoras/eclipse-solar` | `CalculadoraTiemposEclipse.razor` |
| `/calculadoras/eclipse` | `CalculadoraEclipse.razor` |
| `/calculadoras/fov` | `CalculadoraFOV.razor` |
| `/senales` | `Senales.razor` — noticias WP + RSS + TTS |
| `/planificacion` | `Planificacion.razor` — **privada** (AuthGuard) |
| `/historial` | `Historial.razor` — **privada** (AuthGuard) |
| `/mapa`, `/configuracion/ubicaciones` | `GestionUbicaciones.razor` |
| `/login`, `/confirm-otp` | `Login.razor`, `ConfirmOtp.razor` |
| `/reset-password` | `ResetPassword.razor` — recuperación por código OTP (plantilla "Reset Password" de Supabase con `{{ .Token }}`) |
| `/configuracion` | `Configuracion.razor` |
| `/about` | `AboutPage.razor` |

---

## Lo que NO hacer (anti-patrones críticos)

```
❌ Hardcodear texto en español en .razor
❌ Usar Preferences / SecureStorage / Geolocation directamente en Shared
❌ new HttpClient() — usar IHttpClientFactory
❌ AutoConnectRealtime = true
❌ Lógica astronómica en ViewModels o componentes Razor
❌ Añadir calculadoras al menú lateral
❌ Inventar archivos sin verificar su existencia real
```

---

## Tests

```bash
# Ejecutar todos los tests
dotnet test ObservApp.Shared.Tests/

# Build completo
dotnet build ObservApp.slnx
```

---

## CI/CD

- `build.yml` → Android APK + Windows MSIX + Web WASM
- `azure-static-web-apps-*.yml` → deploy automático en Azure Static Web Apps (plan Free, límite 250 MB)
- Versiones: GitVersion 6.3 por tags `vX.Y.Z`
- MSIX local: `dotnet publish ObservApp/ObservApp.csproj -f net10.0-windows10.0.26100.0 -c Release -p:PublishProfile=MSIX-win-x64`. Los perfiles usan `RuntimeIdentifierOverride` (nunca `RuntimeIdentifier`, que contamina el TFM Android: NU1102 o DLL de Shared obsoleta). `Package.appxmanifest` debe mantener `Version="0.0.0.0"` (lo rellena GitVersion); el asistente "Crear paquetes de aplicación" de VS lo sobrescribe, revertirlo
