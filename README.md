# 🔭 ObservApp

**Aplicación multiplataforma para astrofotografía y observación astronómica visual**, construida con **.NET 10**, **.NET MAUI** y **Blazor WebAssembly**.

Disponible como app nativa (Android y Windows) y como aplicación web progresiva, compartiendo toda la lógica y componentes UI gracias a una arquitectura de proyecto compartido con abstracción de plataforma mediante inyección de dependencias.

**Estado actual:** Arquitectura base establecida · Geolocalización multiplataforma · Calculadora Sol y Luna · Suite de 8 idiomas completa · Autenticación con Supabase · Persistencia de sesiones de observación · Versionado automático con GitVersion.

---

## ✨ Funcionalidades

| Módulo | Descripción | Estado |
|---|---|---|
| 🏠 **Inicio** | Panel de bienvenida y acceso rápido | ✅ Disponible |
| 🗺️ **Mapa** | Selección de ubicación de observación | 🔜 Próximamente |
| 📅 **Planificación** | Crear y gestionar sesiones de observación | ✅ Disponible |
| 🌌 **Efemérides** | Datos de posición de cuerpos celestes | ✅ Disponible |
| 🧮 **Calculadoras** | Suite de herramientas de cálculo astronómico | ✅ Disponible |
| 📂 **Historial** | Registro de sesiones anteriores | ✅ Disponible |
| ⚙️ **Configuración** | Idioma, tema y preferencias | ✅ Disponible |
| 👤 **Autenticación** | Login seguro con Supabase | ✅ Disponible |

### 🧮 Calculadoras incluidas

- ✅ **Sol y Luna** — Tiempos de salida/puesta/tránsito, crepúsculos, fases lunares, eventos anuales
- ✅ **Tiempos de eclipse** — Cálculo de contactos, fases y duración del eclipse solar; incluye avisos sonoros de cada evento del eclipse.
- ✅ **Exposición para eclipses** — Tiempos de exposición por fase usando la fórmula NASA Q
- ✅ **Campo visual (FOV)** — FOV real y aparente para combinaciones telescopio‑ocular
- 🔜 **Aumento y pupila de salida** — Magnificación óptima según equipo *(próximamente)*
- 🔜 **Límite de resolución** — Criterios de Rayleigh y Dawes *(próximamente)*
- 🔜 **Escala de placa** — arcsec/px y campo total del setup de astrofotografía *(próximamente)*

### 🌌 Efemérides

**Catálogo astronómico interactivo** con datos en tiempo real de cuerpos celestes:

- ✅ **Posiciones planetarias** — Altitud, azimut, distancia, brillo e iluminación en cualquier fecha/ubicación
- ✅ **Datos de la Luna** — Fase lunar, iluminación, posición y distancia
- ✅ **Catálogo DSO** — Objetos de cielo profundo (galaxias, nebulosas, cúmulos)
  - 📂 Datos embebidos en: `ObservApp.Shared/Resources/dso-catalog.json`
  - 🔍 Búsqueda por nombre, tipo y magnitud
  - 📊 Información técnica: coordenadas, tamaño, magnitud aparente
- ✅ **Próximos eclipses** — Banner informativo del próximo eclipse visible en tu ubicación
- ✅ **Bandera de eclipse** — Indicador visual cuando hay un eclipse próximo

**Datos astronómicos precisos:** Usa CosineKitty.AstronomyEngine para cálculos sin conexión a internet.

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
| `pt` | Português (Brasil) |
| `ru` | Русский |
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
| **Supabase** | 1.6.0 | Backend serverless: autenticación, BD PostgreSQL y realtime |
| **GitVersion** | 6.0.0 | Versionado automático basado en tags Git |

---

## 🔐 Autenticación y Base de Datos

### Supabase (Backend Serverless)

ObservApp utiliza **Supabase** para gestión de usuarios y persistencia de datos:

#### Funcionalidades implementadas:

- ✅ **Autenticación segura** — Login/Signup con Supabase Auth (soporta proveedores OAuth)
- ✅ **Base de datos PostgreSQL** — Tablas para sesiones de observación, favoritos y preferencias
- ✅ **Realtime** — Sincronización en tiempo real de cambios
- ✅ **Session management** — Persistencia de tokens y gestión de sesiones
- ✅ **Sesiones de observación** — Crear, editar y recuperar historiales de observación

#### Tablas principales:

```sql
-- Usuarios (gestionados automáticamente por Supabase Auth)
-- users (id, email, created_at, ...)

-- Sesiones de observación
observation_sessions (
  id UUID PRIMARY KEY,
  user_id UUID REFERENCES auth.users,
  location_name TEXT,
  latitude DECIMAL,
  longitude DECIMAL,
  altitude_m INT,
  started_at TIMESTAMP,
  ended_at TIMESTAMP,
  notes TEXT,
  weather_conditions TEXT,
  equipment TEXT,
  objects_observed JSONB,
  created_at TIMESTAMP DEFAULT now()
)

-- Ubicaciones favoritas
favorite_locations (
  id UUID PRIMARY KEY,
  user_id UUID REFERENCES auth.users,
  name TEXT,
  latitude DECIMAL,
  longitude DECIMAL,
  altitude_m INT,
  is_favorite BOOLEAN DEFAULT true,
  created_at TIMESTAMP DEFAULT now()
)
```

#### Acceso desde código:

```csharp
// inyectado en componentes
@inject SupabaseService SupabaseService

// Crear sesión de observación
var newSession = new ObservationSession { ... };
await SupabaseService.Client
  .From("observation_sessions")
  .Insert(newSession);

// Recuperar sesiones del usuario actual
var sessions = await SupabaseService.Client
  .From("observation_sessions")
  .Select("*")
  .Eq("user_id", userId)
  .Order("started_at", ascending: false)
  .Get<List<ObservationSession>>();
```

---

## 📦 Versionado con GitVersion

El número de versión se **genera automáticamente** basándose en tags Git semánticos.

### Cómo funciona:

1. **Tags Git semánticos** — Define la versión usando tags: `v1.3.2`, `v1.4.0`, `v2.0.0`
2. **GitVersion calcula versión** — Determina versión automáticamente en cada compilación
3. **Se inyecta en assembly** — La versión se propaga a todos los binarios

#### Ejemplos de flujo:

```
branch: main
tag: v1.3.2
  ↓
[commit 3 nuevos cambios]
  ↓
dotnet build
  → Version: 1.3.2
  → SemVer: 1.3.2

Después de faire merge en main:
[nuevo tag: v1.4.0 (feature)]
  ↓
dotnet build
  → Version: 1.4.0
  → SemVer: 1.4.0
```

### Incrementar versión:

```powershell
# Feature (incrementa minor): 1.3.2 → 1.4.0
git tag v1.4.0
git push origin v1.4.0

# Bugfix (incrementa patch): 1.3.2 → 1.3.3
git tag v1.3.3
git push origin v1.3.3

# Breaking change (incrementa major): 1.3.2 → 2.0.0
git tag v2.0.0
git push origin v2.0.0
```

### Ver versión actual:

```powershell
# Script local (requiere GitVersion.Tool instalado)
.\Get-Version.ps1

# Salida:
# ✓ Version: 1.3.2
#   Major: 1, Minor: 3
#   Full Version: 1.3.2.0
#   Informational: 1.3.2
#   Branch: develop
#   Commits: 0
```

### Configuración:

El archivo `gitversion.yml` en la raíz controla el comportamiento:

```yaml
configuration:
  tag-prefix: v                    # Tags deben empezar con 'v'
  mode: Mainline                   # Usa estrategia Mainline
  increment: Patch

  branches:
    main:
      mode: Mainline
      increment: Patch
      regex: ^main$|^master$

    develop:
      mode: ContinuousDeployment
      increment: Minor
      regex: ^develop$
```

### En GitHub Actions:

El workflow `.github/workflows/build.yml` automáticamente:

```yaml
- name: Calculate version with GitVersion
  run: dotnet-gitversion /output buildserver /nofetch
  # Establece env vars: GITVERSION_SEMVER, GITVERSION_MAJOR, etc.

- name: Build Android
  run: dotnet build ... 
  # Usa las env vars para versionar el APK
```

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

### Configuración requerida

#### Variables de entorno (Supabase y Syncfusion)

Antes de ejecutar, debes crear archivos `appsettings.Local.json` en:
- `ObservApp/appsettings.Local.json` (para MAUI)
- `ObservApp.Web.Client/wwwroot/appsettings.Local.json` (para Web)

**Plantilla:**

```json
{
  "SupabaseUrl": "https://tu-proyecto.supabase.co",
  "SupabaseAnonKey": "tu-clave-anonima",
  "SyncfusionLicenseKey": "tu-clave-syncfusion"
}
```

#### Obtener credenciales:

1. **Supabase** — Crea proyecto en [supabase.com](https://supabase.com)
   - Ve a: Configuración → API → URL y Anon Key
   - Copia ambas en `appsettings.Local.json`

2. **Syncfusion** — Obtén licencia (community/comercial)
   - Acceso a: [syncfusion.com/products/licenses](https://www.syncfusion.com/products/licenses)
   - Copia clave en `appsettings.Local.json`

> ℹ️ **Nota:** Los archivos `appsettings.Local.json` están en `.gitignore` para proteger las credenciales.

---

## 🔗 Documentación adicional

- **[Guía rápida de Supabase](Documentacion/Supabase/QUICK_START.md)** — Configurar backend, autenticación y BD
- **[Catálogo DSO](Documentacion/DSO-Catalog.md)** — Objetos de cielo profundo y API de acceso
- **[Arquitectura en detalle](Documentacion/DocumentacionTecnica.md)** — Diagramas y patrones de diseño
- **[Desarrollo local](Documentacion/DEVELOPMENT.md)** — Guía paso a paso

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
