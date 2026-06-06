# 🔭 ObservApp

**Aplicación multiplataforma para astrofotografía y observación astronómica visual**, construida con **.NET 10**, **.NET MAUI** y **Blazor WebAssembly**.

Disponible como app nativa (Android y Windows) y como aplicación web progresiva, compartiendo toda la lógica y componentes UI gracias a una arquitectura de proyecto compartido con abstracción de plataforma mediante inyección de dependencias.

**Estado actual:** Arquitectura base establecida · Geolocalización multiplataforma · Calculadora Sol y Luna · Suite de 6 idiomas completa · Localización dinámicamente sincronizada en web.

---

## ✨ Funcionalidades

| Módulo | Descripción | Estado |
|---|---|---|
| 🏠 **Inicio** | Panel de bienvenida y acceso rápido | ✅ Disponible |
| 🗺️ **Mapa** | Selección de ubicación de observación | 🔜 Próximamente |
| 📅 **Planificación** | Crear y gestionar sesiones de observación | 🔜 Próximamente |
| 🌌 **Efemérides** | Datos de posición de cuerpos celestes | 🔜 Próximamente |
| 🧮 **Calculadoras** | Suite de herramientas de cálculo astronómico | ✅ Disponible |
| 📂 **Historial** | Registro de sesiones anteriores | 🔜 Próximamente |
| ⚙️ **Configuración** | Idioma, tema y preferencias | ✅ Disponible |

### 🧮 Calculadoras incluidas

- ✅ **Sol y Luna** — Tiempos de salida/puesta/tránsito, crepúsculos, fases lunares, eventos anuales
- ✅ **Tiempos de eclipse** — Cálculo de contactos, fases y duración del eclipse solar; incluye avisos sonoros de cada evento del eclipse.
- ✅ **Exposición para eclipses** — Tiempos de exposición por fase usando la fórmula NASA Q
- ✅ **Campo visual (FOV)** — FOV real y aparente para combinaciones telescopio‑ocular
- 🔜 **Aumento y pupila de salida** — Magnificación óptima según equipo *(próximamente)*
- 🔜 **Límite de resolución** — Criterios de Rayleigh y Dawes *(próximamente)*
- 🔜 **Escala de placa** — arcsec/px y campo total del setup de astrofotografía *(próximamente)*

### 🗺️ Gestión de Ubicaciones

- ✅ **Ubicaciones favoritas** — Guarda ubicaciones con mapa interactivo Leaflet
- ✅ **Búsqueda de lugares** — Integración con Nominatim (OpenStreetMap)
- ✅ **Consulta automática de altitud** — API Open-Meteo Elevation
- ✅ **Geolocalización** — GPS/navegador para ubicación actual

---

## 🏗️ Arquitectura

```
ObservApp/
├── ObservApp/              # .NET MAUI — Android & Windows (nativa)
├── ObservApp.Web/          # Blazor Server — host SSR + API
├── ObservApp.Web.Client/   # Blazor WebAssembly — cliente interactivo
└── ObservApp.Shared/       # Lógica compartida: páginas, componentes, servicios, i18n, ViewModels, state
```

### Principios de diseño

- **Código compartido al máximo**: todas las páginas y componentes Razor viven en `ObservApp.Shared` (Razor Class Library) y son consumidos por MAUI y Web sin duplicación.
- **Abstracción de plataforma**: interfaces (`ISettingsService`, `ILocalizationService`, `IGeolocationService`) con implementaciones específicas por plataforma:
  - **MAUI:** `MauiSettingsService` (Preferences nativo), `LocalizationService` (cambio dinámico de idioma), `MauiGeolocationService` (IGeolocation)
  - **Web:** `WebSettingsService` (localStorage vía JSInterop), `WebLocalizationService` (CultureInfo en cliente), `WebGeolocationService` (Geolocation API del navegador)
  - **SSR:** `SsrGeolocationService` (stub — sin acceso GPS en servidor)
- **Estado centralizado**: `AppState` como servicio singleton con notificación reactiva de cambios (`OnStateChanged`).
- **Patrón MVVM con CommunityToolkit.Mvvm:** ViewModels reutilizables (`BaseViewModel`, `HistorialViewModel`, `ConfiguracionViewModel`) con source generators para propiedades observables.
- **Router multi-ensamblado:** Soporte para páginas `@page` tanto en `ObservApp.Shared` como en proyectos hosts.

---

## 🌍 Idiomas soportados

| Código | Idioma |
|---|---|
| `es` | Español (predeterminado) |
| `en` | English |
| `fr` | Français |
| `de` | Deutsch |
| `it` | Italiano |
| `ar` | العربية |

El idioma se persiste automáticamente: en web mediante `localStorage`, en MAUI mediante las preferencias nativas del dispositivo.

---

## 🛠️ Stack tecnológico

| Tecnología | Versión | Uso |
|---|---|---|
| **.NET** | 10 | Framework base |
| **.NET MAUI** | 10 | App nativa Android / Windows |
| **Blazor WebAssembly** | .NET 10 | App web interactiva (cliente) |
| **Blazor SSR** | .NET 10 | Host web con renderizado en servidor |
| **CommunityToolkit.Mvvm** | 8.4.0 | ViewModels con source generators |
| **Syncfusion Blazor** | Última | Componentes UI avanzados |
| **Leaflet.js** | Última | Mapas interactivos para ubicaciones |
| **CosineKitty.AstronomyEngine** | Última | Cálculos astronómicos offline precisos |
| **JSInterop** | .NET 10 | Acceso a APIs del navegador desde .NET |

---

## 🚀 Ejecución rápida

### ⚠️ Importante: ¿Qué proyecto ejecutar?

**ObservApp** tiene dos proyectos web:
- **`ObservApp.Web`** ← **Este es el que debes ejecutar** (servidor + host)
- **`ObservApp.Web.Client`** ← Cliente WASM (se carga automáticamente en el navegador)

**Siempre ejecuta `ObservApp.Web` como proyecto de inicio.** El cliente se descarga automáticamente cuando abres el navegador.

---

### Opción 1: Scripts automatizados (Recomendado)

**Versión Web:**
```powershell
# PowerShell
.\start-web.ps1

# O con CMD
start-web.bat
```

El navegador se abrirá automáticamente en `https://localhost:7294`

**Versión MAUI:**
```powershell
# Desde Visual Studio: establecer ObservApp como proyecto de inicio y presionar F5
```

### Opción 2: Línea de comandos manual

**Versión Web:**
```powershell
cd ObservApp.Web
dotnet watch run --launch-profile https
```

**Versión MAUI (Android):**
```powershell
dotnet build ObservApp -f net10.0-android
# Luego desplegar desde Visual Studio al emulador o dispositivo
```

### Opción 3: Visual Studio 2026

1. **Abrir** `ObservApp.slnx` en Visual Studio 2026
2. **Establecer proyecto de inicio:**
   - Para Web: Clic derecho en `ObservApp.Web` → "Establecer como proyecto de inicio"
   - Para MAUI: Clic derecho en `ObservApp` → "Establecer como proyecto de inicio"
3. **Seleccionar perfil de depuración:**
   - Web: Seleccionar `https` en el desplegable superior
   - MAUI: Seleccionar `Windows Machine` o dispositivo Android
4. **Presionar F5** para ejecutar

**Tip:** Puedes configurar proyectos de inicio múltiples para ejecutar Web y MAUI simultáneamente:
- Clic derecho en la solución → Propiedades → Proyectos de inicio → Proyectos de inicio múltiples

---

## 🚀 Requisitos y compilación

### Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2026](https://visualstudio.microsoft.com/) con las cargas de trabajo:
  - **.NET MAUI** (para la app nativa)
  - **ASP.NET y desarrollo web** (para la app web)
- Android SDK (API 24+) para el target Android

### Clonar y compilar

```powershell
git clone https://github.com/ebenito/ObservApp.git
cd ObservApp
dotnet restore
dotnet build
```

---

## ⚠️ Solución de problemas

### La versión web no abre el navegador

Consulta la guía detallada: [Documentacion/guia-solucion-web-no-abre.md](Documentacion/guia-solucion-web-no-abre.md)

**Solución rápida:**
1. Verificar que `ObservApp.Web` es el proyecto de inicio en Visual Studio
2. Verificar que el perfil `https` está seleccionado
3. O ejecutar desde terminal: `.\start-web.ps1`

### El mapa no se muestra después de búsqueda

Consulta: [Documentacion/solucion-problemas-ubicaciones.md](Documentacion/solucion-problemas-ubicaciones.md)

**Solución:** Ya está corregido en la última versión. El mapa se inicializa automáticamente si no existe.

### Errores de compilación con Syncfusion

Asegúrate de calificar completamente `ChangeEventArgs` cuando Syncfusion esté presente:
```csharp
private void OnChanged(Microsoft.AspNetCore.Components.ChangeEventArgs e)
```

---

## 📱 Plataformas soportadas

| Plataforma | Versión mínima |
|---|---|
| Android | 7.0 (API 24) |
| Windows | Windows 10 1803 (build 17763) |
| Web | Cualquier navegador moderno con soporte WebAssembly |

---

## 📁 Estructura del proyecto compartido

```
ObservApp.Shared/
├── Layout/               # MainLayout, NavMenu
├── Pages/                # Páginas Razor (Home, CalculadoraSolLuna, Configuracion…)
├── Components/           # Componentes reutilizables
├── Models/               # ObservationSession, ObservationTarget, enumeraciones
├── Resources/Strings/    # Archivos .resx de localización (es, en, fr, de, it, ar)
├── Services/             # Interfaces: ISettingsService, ILocalizationService, IGeolocationService
├── State/                # AppState — estado global reactivo
├── ViewModels/           # BaseViewModel, HistorialViewModel, ConfiguracionViewModel
└── wwwroot/
    ├── app.css
    └── geolocation.js    # JSInterop wrapper para Geolocation API
```

---

## 📚 Documentación técnica completa

Para entender la arquitectura, refactorizaciones, modelos de dominio, patrones MVVM, servicios de plataforma y TODOs pendientes, consulta la documentación técnica:

**→ [Documentacion/DocumentacionTecnica.md](Documentacion/DocumentacionTecnica.md)**

---

## 📝 Historial de cambios

El historial de cambios y releases se mantiene en la carpeta `Documentacion/changelog/`:

| Archivo | Descripción |
|---------|-------------|
| [2026_07-calculadora-tiempos-eclipse.md](Documentacion/changelog/2026_07-calculadora-tiempos-eclipse.md) | Feature: Calculadora de tiempos de eclipse solar con avisos sonoros y simulación en tiempo real |
| [2026_06-refactoring-arquitectura-completa.md](Documentacion/changelog/2026_06-refactoring-arquitectura-completa.md) | Refactoring: arquitectura con abstracción de servicios, MVVM, geolocalización multiplataforma |
| [2026_05-fix-cambio-idioma-sidebar.md](Documentacion/changelog/2026_05-fix-cambio-idioma-sidebar.md) | Fix: cambio de idioma y menú hamburguesa inoperativos en Blazor WASM |
| [2026_04-arquitectura-base.md](Documentacion/changelog/2026_04-arquitectura-base.md) | Arquitectura base inicial |

---

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor, abre un *issue* antes de enviar un *pull request* para discutir los cambios propuestos.

---

## 📄 Licencia

Este proyecto está bajo la licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más detalles.
