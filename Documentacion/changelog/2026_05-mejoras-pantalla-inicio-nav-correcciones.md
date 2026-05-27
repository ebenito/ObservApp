# Mejoras pantalla de inicio, navegación y correcciones varias

> **Rama:** `master`
> **Fecha:** 2026-05-XX
> **Scope:** `ObservApp.Shared` · `ObservApp` · `ObservApp.Web` · `ObservApp.Web.Client`

---

## Contexto

Sesión de revisión general de la aplicación antes de avanzar con las pantallas pendientes del menú.
Se identificaron y corregieron varios bugs y se rediseñó por completo la pantalla de inicio, que
hasta el momento sólo mostraba un `<h1>` y una línea de bienvenida.

---

## Cambios

### 1 · Fix: textos hardcodeados en `Routes.razor` — localización rota

**Archivo:** `ObservApp.Shared/Routes.razor`

**Problema:**
El bloque `<NotFound>` del router tenía los tres textos de "página no encontrada" escritos
directamente en español, ignorando completamente el sistema `IStringLocalizer` utilizado en
el resto de la aplicación.

**Fix:**
Se añadió `@inject IStringLocalizer<ObservApp.Resources.Strings.App> L` y se sustituyeron los
tres literales por las claves de recurso existentes:

| Literal anterior | Clave usada |
|---|---|
| `"No encontrado"` | `L["NotFound_Title"]` |
| `"Página no encontrada"` | `L["NotFound_Title"]` |
| `"El contenido que buscas no existe o ha sido movido."` | `L["NotFound_Message"]` |
| `"← Volver al inicio"` | `L["NotFound_Back"]` |

Las claves ya existían en los 6 archivos `.resx` por lo que no fue necesario añadir strings nuevos.

---

### 2 · Fix: código muerto en `Configuracion.razor`

**Archivo:** `ObservApp.Shared/Pages/Configuracion.razor`

**Problema:**
El campo `_showConfirmation` y el método `HideConfirmationAsync` existían en el código pero
nunca se usaban: `_showConfirmation` nunca se ponía a `true` por ningún camino de ejecución,
y `HideConfirmationAsync` nunca era invocado. El bloque `else if (_showConfirmation)` del
markup de feedback tampoco se mostraba jamás.

**Fix:**
Se eliminaron los tres elementos muertos:

- Campo `private bool _showConfirmation`
- Método `private async Task HideConfirmationAsync()`
- Bloque `else if (_showConfirmation) { … }` en el markup del toast

---

### 3 · Fix: ruta inconsistente de la Calculadora FOV

**Archivos:** `ObservApp.Shared/Pages/CalculadoraFOV.razor` · `ObservApp.Shared/Pages/Calculadoras.razor`

**Problema:**
La Calculadora de Campo Visual (FOV) tenía su ruta definida como `/calculadora-fov`, fuera
de la jerarquía `/calculadoras/…`. Esto causaba dos efectos negativos:

1. El método `IsActive("/calculadoras")` en el `MainLayout` no marcaba la sección activa
   al navegar dentro de la calculadora FOV.
2. Inconsistencia visual y semántica respecto a la Calculadora de Eclipses, que sí usaba
   la ruta `/calculadoras/eclipse`.

**Fix:**

| Archivo | Antes | Después |
|---|---|---|
| `CalculadoraFOV.razor` | `@page "/calculadora-fov"` | `@page "/calculadoras/fov"` |
| `Calculadoras.razor` | `href="/calculadora-fov"` | `href="/calculadoras/fov"` |

El enlace "← Volver" dentro de `CalculadoraFOV.razor` ya apuntaba a `/calculadoras`
y no requirió cambios.

---

### 4 · Nueva funcionalidad: propiedad `CanExit` en `IAppLifecycleService`

**Archivos afectados:**

| Archivo | Cambio |
|---|---|
| `ObservApp.Shared/Services/IAppLifecycleService.cs` | Nueva propiedad `bool CanExit` en la interfaz |
| `ObservApp/Services/MauiAppLifecycleService.cs` | `CanExit => true` |
| `ObservApp.Web/Services/WebAppLifecycleService.cs` | `CanExit => false` |
| `ObservApp.Web.Client/Services/WebClientAppLifecycleService.cs` | `CanExit => false` |
| `ObservApp.Shared/Layout/MainLayout.razor` | Botón "Salir" envuelto en `@if (AppLifecycle.CanExit)` |

**Motivación:**
En las versiones web (Blazor Server SSR y Blazor WASM) el botón "Salir" del menú hamburguesa
no tiene efecto útil — `window.close()` sólo funciona si el navegador abrió la pestaña
mediante script. Para evitar confusión al usuario, la opción se oculta en web y se mantiene
visible únicamente en la versión MAUI (Android, iOS, Windows, Mac Catalyst).

**Implementación:**
Se optó por añadir la propiedad a la interfaz en lugar de usar una detección de plataforma en
el componente Razor, manteniendo el código compartido desacoplado de cualquier API nativa.

---

### 5 · Rediseño completo de la pantalla de inicio

**Archivos:**

| Archivo | Estado |
|---|---|
| `ObservApp.Shared/Pages/Home.razor` | Reescrito completamente |
| `ObservApp.Shared/Pages/Home.razor.css` | Nuevo (CSS scoped) |
| `ObservApp.Shared/Resources/Strings/App.es.resx` | +14 claves nuevas |
| `ObservApp.Shared/Resources/Strings/App.en.resx` | +14 claves nuevas |
| `ObservApp.Shared/Resources/Strings/App.de.resx` | +14 claves nuevas |
| `ObservApp.Shared/Resources/Strings/App.fr.resx` | +14 claves nuevas |
| `ObservApp.Shared/Resources/Strings/App.it.resx` | +14 claves nuevas |
| `ObservApp.Shared/Resources/Strings/App.ar.resx` | +14 claves nuevas |

**Antes:** La pantalla de inicio consistía únicamente en un `<h1>` con el título y una línea
de texto de bienvenida. No había ningún contenido, acceso rápido ni imagen.

**Después:** Pantalla completa con tres secciones:

#### 5.1 Hero con ilustración SVG animada

Ilustración vectorial inline (sin dependencias externas) que representa un cielo nocturno con:

- Degradado de fondo azul-noche usando los tokens de diseño de ObservApp
- Luna llena con halo dorado y cráteres
- Nebulosa difuminada en tonos aurora/púrpura
- 17 estrellas con animación CSS de centelleo independiente (pares e impares con fase distinta)
- Estrella brillante con destellos en cruz tipo Vega/Sirio con filtro SVG `feMerge`
- Silueta de telescopio refractor en trípode con animación de flotación suave (5 s, ease-in-out)
  apuntando hacia la luna con un haz de luz discontinuo
- Silueta de montaña en el horizonte
- Vía Láctea difusa como ellipses rotadas con `filter:blur`

Sobre la ilustración se superpone un texto hero con badge de marca, título principal y subtítulo
descriptivo de la app, todos localizados.

#### 5.2 Grid de acceso rápido

Cuatro tarjetas de navegación directa a las secciones principales:

| Tarjeta | Ruta | Color de acento |
|---|---|---|
| 🗺️ Mapa | `/mapa` | `--obs-aurora` (teal) |
| 📅 Planificación | `/planificacion` | `--obs-star` (dorado) |
| 🌙 Efemérides | `/efemerides` | `--obs-nebula` (púrpura) |
| 🧮 Calculadoras | `/calculadoras` | `--obs-mars` (naranja) |

En móvil se muestran en columna; en pantallas ≥ 520 px se adaptan a grid 2×2 con layout
vertical de cada tarjeta.

#### 5.3 Grid de características

Grid 2×2 con cuatro bloques informativos sobre las capacidades de la app:
Observación visual, Calculadoras, Astrofotografía e Historial.

#### 5.4 Strings nuevos localizados

Las 14 claves nuevas se añadieron a los 6 idiomas soportados (es, en, de, fr, it, ar):

`Home_Subtitle` · `Home_QuickAccess` · `Home_Card_Map_Desc` · `Home_Card_Planning_Desc` ·
`Home_Card_Ephemeris_Desc` · `Home_Card_Calculators_Desc` · `Home_Features_Title` ·
`Home_Feature1_Title` · `Home_Feature1_Desc` · `Home_Feature2_Title` · `Home_Feature2_Desc` ·
`Home_Feature3_Title` · `Home_Feature3_Desc` · `Home_Feature4_Title` · `Home_Feature4_Desc`

---

## Pendiente

A continuación se listan las pantallas y funcionalidades identificadas como pendientes
en esta misma sesión de revisión:

### Pantallas del menú sin implementar

Estas cinco rutas están presentes en el menú hamburguesa (`MainLayout.razor`) pero no tienen
página asociada. Navegar a ellas muestra actualmente el componente `NotFound`.

| Ruta | Descripción |
|---|---|
| `/mapa` | Mapa de lugares de observación con cielos oscuros |
| `/planificacion` | Creación y gestión de sesiones de observación |
| `/efemerides` | Posición de planetas, salida/puesta de Sol y Luna |
| `/historial` | Registro de sesiones pasadas |
| `/about` | Información de la aplicación, versión y créditos |

### Mejoras de código pendientes

| Elemento | Descripción |
|---|---|
| `AppState.IsAuthenticated` / `UserDisplayName` | Propiedades definidas en `AppState.cs` pero sin ningún consumidor. Eliminar o implementar cuando se defina el modelo de usuario. |
| `Nav_EclipseCalc` | Clave de recurso presente en todos los `.resx` pero sin ninguna referencia en los `.razor`. Eliminar o conectar al menú si se añade acceso directo desde la navegación. |
| `WebSettingsService` localStorage | Issue #1 cerrado en la sesión anterior, pero conviene verificar que la persistencia funciona correctamente en producción (HTTPS, cookies de terceros desactivadas). |
| Calculadoras pendientes | En el hub `/calculadoras` hay tres tarjetas marcadas como "Próximamente": **Aumento y pupila de salida**, **Límite de resolución** y **Escala de placa**. |
