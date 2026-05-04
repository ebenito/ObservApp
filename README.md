# 🔭 ObservApp

**Aplicación multiplataforma para astrofotografía y observación astronómica visual**, construida con **.NET 10**, **.NET MAUI** y **Blazor WebAssembly**.

Disponible como app nativa (Android y Windows) y como aplicación web progresiva, compartiendo toda la lógica y componentes UI gracias a una arquitectura de proyecto compartido.

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

- **Exposición para eclipses** — Tiempos de exposición por fase usando la fórmula NASA Q
- **Campo visual (FOV)** — FOV real y aparente para combinaciones telescopio‑ocular *(próximamente)*
- **Aumento y pupila de salida** — Magnificación óptima según equipo *(próximamente)*
- **Límite de resolución** — Criterios de Rayleigh y Dawes *(próximamente)*
- **Escala de placa** — arcsec/px y campo total del setup de astrofotografía *(próximamente)*

---

## 🏗️ Arquitectura

```
ObservApp/
├── ObservApp/              # .NET MAUI — Android & Windows (nativa)
├── ObservApp.Web/          # Blazor Server — host SSR + API
├── ObservApp.Web.Client/   # Blazor WebAssembly — cliente interactivo
└── ObservApp.Shared/       # Lógica compartida: páginas, componentes, servicios, i18n
```

### Principios de diseño

- **Código compartido al máximo**: todas las páginas y componentes Razor viven en `ObservApp.Shared` y son consumidos por MAUI y Web sin duplicación.
- **Abstracción de plataforma**: interfaces (`ISettingsService`, `ILocalizationService`) con implementaciones específicas por plataforma (MAUI Preferences, localStorage/JSInterop, SSR).
- **Estado centralizado**: `AppState` como servicio singleton con notificación reactiva de cambios.

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

| Tecnología | Uso |
|---|---|
| **.NET 10** | Framework base |
| **.NET MAUI** | App nativa Android / Windows |
| **Blazor WebAssembly** | App web interactiva (cliente) |
| **Blazor SSR** | Host web con renderizado en servidor |
| **Syncfusion Blazor** | Componentes UI avanzados |
| **JSInterop** | Acceso a APIs del navegador desde .NET |

---

## 🚀 Requisitos y compilación

### Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) con las cargas de trabajo:
  - **.NET MAUI** (para la app nativa)
  - **ASP.NET y desarrollo web** (para la app web)
- Android SDK (API 24+) para el target Android

### Clonar y compilar

```powershell
git clone https://github.com/ebenito/ObservApp.git
cd ObservApp
dotnet restore
```

### Ejecutar la versión web

```powershell
dotnet run --project ObservApp.Web
```

### Ejecutar la app MAUI (Android)

```powershell
dotnet build ObservApp -f net10.0-android
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
├── Layout/             # MainLayout, NavMenu
├── Pages/              # Páginas Razor (Home, Calculadoras, Configuracion…)
├── Resources/Strings/  # Archivos .resx de localización (es, en, fr, de, it, ar)
├── Services/           # Interfaces ISettingsService, ILocalizationService
└── State/              # AppState — estado global reactivo
```

---

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor, abre un *issue* antes de enviar un *pull request* para discutir los cambios propuestos.

---

## 📄 Licencia

Este proyecto está bajo la licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más detalles.
