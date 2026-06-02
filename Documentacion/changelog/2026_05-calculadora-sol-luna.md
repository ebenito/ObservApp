# Calculadora Sol y Luna + Gestión de Ubicaciones Favoritas

> **Rama:** `master`
> **Fecha:** 2026-05
> **Scope:** `ObservApp.Shared` · `ObservApp` · `ObservApp.Web` · `ObservApp.Web.Client`

---

## Contexto

**Parte 1:** Se añade una nueva calculadora astronómica de Sol y Luna a la sección de calculadoras de la app,
comparable en funcionalidad a aplicaciones como LunaSolCal. Proporciona tiempos de salida/tránsito/puesta,
crepúsculos, fase lunar, eventos anuales y gráficos de trayectoria, todo de forma offline usando la
librería `CosineKitty.AstronomyEngine` ya presente en el proyecto.

**Parte 2:** Se implementa un sistema completo de gestión de ubicaciones favoritas con mapa interactivo
Leaflet, búsqueda de lugares vía Nominatim y consulta automática de altitud mediante Open-Meteo.

En la misma sesión se introduce un servicio de geolocalización reutilizable con implementaciones
separadas para MAUI y Web, y se corrige un error de serialización del `CancellationToken` en el
cliente WebAssembly.

---

## Cambios

### 1 · Nueva página — Calculadora Sol y Luna

**Archivos:**
- `ObservApp.Shared/Pages/CalculadoraSolLuna.razor` 🆕
- `ObservApp.Shared/Pages/CalculadoraSolLuna.razor.css` 🆕

**Funcionalidad:**

| Panel | Datos mostrados |
|---|---|
| ☀️ Sol | Salida · Tránsito · Puesta · Duración del día · Crepúsculos civil / náutico / astronómico (amanecer y atardecer) · Azimuts · Altitud máxima · Declinación · Ascensión recta · Ecuación del tiempo · Distancia en UA · Gráfico de trayectoria horaria |
| 🌙 Luna | Salida · Tránsito · Puesta · Azimuts · Altitud máxima · Distancia · Diámetro angular · Iluminación · Ángulo de fase · Edad lunar · Nombre y emoji de fase · Próximas luna nueva y llena · Gráfico de trayectoria horaria |
| 🗓️ Eventos | Equinoccios y solsticios del año · Listado completo de fases lunares del año (luna nueva, cuarto creciente, llena, menguante) ordenadas cronológicamente |

**Motor de cálculo:** `CosineKitty.AstronomyEngine` (offline, sin llamadas a API externas).

**Entradas del usuario:**
- Fecha (selector nativo `<input type="date">`)
- Latitud, longitud y altitud (edición manual + botón GPS)

**Ruta:** `/calculadoras/soluna`

---

### 2 · Hub de calculadoras actualizado

**Archivo:** `ObservApp.Shared/Pages/Calculadoras.razor` ✏️

Se añade la tarjeta de la nueva calculadora Sol y Luna en la sección de calculadoras disponibles,
usando las claves de localización `Calc_SolLuna_Title` y `Calc_SolLuna_Desc`.

---

### 3 · Localización — nuevas claves

**Archivos:** `App.es.resx`, `App.en.resx`, `App.de.resx`, `App.fr.resx`, `App.it.resx`, `App.ar.resx` ✏️

Claves añadidas en los seis idiomas:

| Clave | ES |
|---|---|
| `Calc_SolLuna_Title` | Sol y Luna |
| `Calc_SolLuna_Desc` | Salida, tránsito y puesta del Sol y la Luna, fases, crepúsculos y eventos anuales |
| `SolLuna_Title` | Calculadora Sol y Luna |
| `SolLuna_Subtitle` | Horarios, fases y eventos astronómicos |
| `SolLuna_Date` | Fecha |
| `SolLuna_Location` | Ubicación |
| `SolLuna_Latitude` | Latitud |
| `SolLuna_Longitude` | Longitud |
| `SolLuna_Altitude` | Altitud |
| `SolLuna_UseGPS` | Usar GPS |
| `SolLuna_Tab_Sun` | Sol |
| `SolLuna_Tab_Moon` | Luna |
| `SolLuna_Tab_Events` | Eventos del año |
| … | (y demás claves de etiquetas de resultado) |

---

### 4 · Servicio de geolocalización reutilizable

#### 4.1 Interfaz compartida

**Archivo:** `ObservApp.Shared/Services/IGeolocationService.cs` 🆕

```csharp
public record LocationData(
	double Latitude,
	double Longitude,
	double? AltitudeMeters,
	double? AccuracyMeters,
	string SourceLabel);

public interface IGeolocationService
{
	string? LastError { get; }
	Task<LocationData?> GetCurrentLocationAsync(
		bool highAccuracy = true,
		CancellationToken cancellationToken = default);
}
```

La interfaz se define en la capa compartida para que cualquier página de `ObservApp.Shared`
pueda inyectarla sin acoplarse a la plataforma.

#### 4.2 Implementación MAUI

**Archivo:** `ObservApp/Services/MauiGeolocationService.cs` 🆕

Usa `Microsoft.Maui.Devices.Sensors.IGeolocation` (plataforma MAUI).
Solicita el permiso `Permissions.LocationWhenInUse` antes de cada petición.
Mapea los errores de plataforma (`FeatureNotSupportedException`, `FeatureNotEnabledException`,
`PermissionException`, `OperationCanceledException`) a mensajes legibles en `LastError`.

Registrada en `ObservApp/MauiProgram.cs`:
```csharp
builder.Services.AddSingleton(Geolocation.Default);
builder.Services.AddSingleton<IGeolocationService, MauiGeolocationService>();
```

**Permisos añadidos:**
- Android: `ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION` en `AndroidManifest.xml`
- iOS/macOS: `NSLocationWhenInUseUsageDescription` en `Info.plist`

#### 4.3 Implementación Web (WebAssembly)

**Archivo:** `ObservApp.Web.Client/Services/WebGeolocationService.cs` 🆕

Usa `IJSRuntime` para invocar `window.observApp.getCurrentPosition(highAccuracy)`.
Mapea el resultado JS al record `LocationData`. Gestiona errores `JSException` y cancelación.

Registrada en `ObservApp.Web.Client/Program.cs`:
```csharp
builder.Services.AddSingleton<IGeolocationService, WebGeolocationService>();
```

#### 4.4 Stub SSR (servidor)

**Archivo:** `ObservApp.Web/Services/SsrGeolocationService.cs` 🆕

Implementación vacía que devuelve `null`. Necesaria para que el DI del servidor SSR pueda
resolver `IGeolocationService` aunque en ese contexto no haya acceso a GPS.

Registrada en `ObservApp.Web/Program.cs`:
```csharp
builder.Services.AddScoped<IGeolocationService, SsrGeolocationService>();
```

#### 4.5 Helper JavaScript

**Archivo:** `ObservApp.Shared/wwwroot/geolocation.js` 🆕

```js
window.observApp.getCurrentPosition = function (highAccuracy) {
	return new Promise(function (resolve) {
		navigator.geolocation.getCurrentPosition(
			pos => resolve({ latitude, longitude, altitude, accuracy, error: null }),
			err => resolve({ ..., error: mensajeLocalizado }),
			{ enableHighAccuracy, timeout: 15000, maximumAge: 30000 }
		);
	});
};
```

Cargado en `ObservApp.Web/Components/App.razor`:
```html
<script src="_content/ObservApp.Shared/geolocation.js"></script>
```

---

### 5 · Fix: CancellationToken serializado como argumento JS

**Archivo:** `ObservApp.Web.Client/Services/WebGeolocationService.cs` ✏️

**Problema:**
Al invocar `_js.InvokeAsync<T>("función", highAccuracy, cancellationToken)`, el `CancellationToken`
se pasaba como argumento posicional y Blazor intentaba serializarlo a JSON. El `IntPtr` interno
del `WaitHandle` no es serializable, lanzando:

```
SerializeTypeInstanceNotSupported, System.IntPtr Path: $.WaitHandle.Handle
```

**Fix:**
Se usa la sobrecarga correcta que separa el token de los argumentos JS:

```csharp
// ❌ Antes
await _js.InvokeAsync<BrowserLocation?>("observApp.getCurrentPosition", highAccuracy, cancellationToken);

// ✅ Después
await _js.InvokeAsync<BrowserLocation?>("observApp.getCurrentPosition", cancellationToken, new object[] { highAccuracy });
```

---

### 6 · Sistema de Gestión de Ubicaciones Favoritas

#### 6.1 Modelo y servicio compartido

**Archivos:**
- `ObservApp.Shared/Models/FavoriteLocation.cs` 🆕
- `ObservApp.Shared/Services/IFavoriteLocationsService.cs` 🆕

Modelo de datos:
```csharp
public class FavoriteLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AltitudeMeters { get; set; }
}
```

Interfaz de servicio con métodos `GetFavoriteLocationsAsync()`, `SaveFavoriteLocationAsync()`, `DeleteFavoriteLocationAsync()`.

#### 6.2 Implementaciones por plataforma

| Proyecto | Clase | Mecanismo |
|---|---|---|
| MAUI | `MauiFavoriteLocationsService` | `Microsoft.Maui.Storage.Preferences` (JSON serializado) |
| Web WASM | `WebFavoriteLocationsService` | `localStorage` via JSInterop |
| Web SSR | `SsrFavoriteLocationsService` | Stub que devuelve lista vacía |

Registrados en `MauiProgram.cs`, `ObservApp.Web.Client/Program.cs` y `ObservApp.Web/Program.cs` respectivamente.

#### 6.3 Página de gestión con mapa interactivo

**Archivo:** `ObservApp.Shared/Pages/GestionUbicaciones.razor` 🆕

**Funcionalidades:**
- **Mapa interactivo Leaflet** con marcador arrastrable que actualiza lat/lon en tiempo real
- **Búsqueda de lugares** mediante API de Nominatim (OpenStreetMap)
- **Consulta automática de altitud** vía API Open-Meteo Elevation al mover el marcador o seleccionar búsqueda
- **Botón GPS** para capturar ubicación actual del dispositivo/navegador
- **Listado de ubicaciones guardadas** con edición y eliminación
- **Formulario de detalles** con validación (nombre requerido)

**Ruta:** `/configuracion/ubicaciones`

**Helper JavaScript:** `ObservApp.Shared/wwwroot/leaflet-map.js` 🆕
```javascript
window.observApp.initMap(containerId, lat, lon, zoom, dotNetRef)
window.observApp.updateMapPosition(containerId, lat, lon, zoom)
```

#### 6.4 Integración en Calculadora Sol y Luna

**Archivo:** `ObservApp.Shared/Pages/CalculadoraSolLuna.razor` ✏️

Se añade selector desplegable en la sección de ubicación con tres opciones:
- 📍 Manual (edición manual de coordenadas)
- 📡 GPS Actual (llama a `OnUsarGpsAsync()`)
- ⭐ [Ubicaciones favoritas guardadas] (carga lat/lon/alt del favorito)

Al seleccionar una ubicación favorita, se actualizan automáticamente las coordenadas y se recalcula la calculadora.

#### 6.5 Acceso desde Configuración

**Archivo:** `ObservApp.Shared/Pages/Configuracion.razor` ✏️

Nueva sección debajo de la selección de idioma:
- Título: **Ubicaciones Guardadas**
- Botón: **Gestionar ubicaciones favoritas 🗺️** → navega a `/configuracion/ubicaciones`

#### 6.6 Leaflet añadido a los hosts

**Archivos:**
- `ObservApp.Web/Components/App.razor` ✏️
- `ObservApp/wwwroot/index.html` ✏️

Se añaden en ambos:
```html
<!-- CSS -->
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" crossorigin="" />

<!-- JS -->
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" crossorigin=""></script>
<script src="_content/ObservApp.Shared/leaflet-map.js"></script>
```

#### 6.7 Localización

**Archivos:** `App.*.resx` (ES/EN/DE/FR/IT/AR) ✏️

Nuevas claves añadidas:
- `Settings_FavoriteLocations`
- `FavLoc_Title`, `FavLoc_SearchPlaceholder`, `FavLoc_SearchBtn`
- `FavLoc_AddCurrent`, `FavLoc_NoLocations`, `FavLoc_Search_NoResults`
- `FavLoc_Form_Name`, `FavLoc_Form_Latitude`, `FavLoc_Form_Longitude`, `FavLoc_Form_Altitude`
- `FavLoc_Form_Name_Required`, `FavLoc_Form_DragHint`, `FavLoc_Form_Altitude_Loading`
- `FavLoc_Select_Manual`, `FavLoc_Select_GPS`
- `FavLoc_Form_Edit`

---

### 7 · Fix: Mapa no se muestra tras búsqueda

**Archivo:** `ObservApp.Shared/Pages/GestionUbicaciones.razor` ✏️

**Problema:**
Cuando el usuario realizaba una búsqueda de lugar antes de que el mapa se renderizara completamente,
`SincronizarMapaPosicionAsync()` intentaba actualizar un mapa inexistente, fallando silenciosamente.

**Fix:**
Se modifica `SincronizarMapaPosicionAsync()` para verificar si el mapa existe antes de actualizar:

```csharp
var existe = await JS.InvokeAsync<bool>("eval", 
    $"!!document.getElementById('leaflet-fav-map')?._leaflet_map");

if (existe)
{
    await JS.InvokeVoidAsync("window.observApp.updateMapPosition", ...);
}
else
{
    // Si no existe, inicializarlo
    await InicializarMapaAsync();
}
```

---

### 8 · Fix: Ambigüedad `ChangeEventArgs` con Syncfusion

**Archivos:**
- `ObservApp.Shared/Pages/CalculadoraSolLuna.razor` ✏️
- `ObservApp.Shared/Pages/GestionUbicaciones.razor` ✏️

**Problema:**
Syncfusion.Blazor.Navigations también define `ChangeEventArgs`, generando conflicto con `Microsoft.AspNetCore.Components.ChangeEventArgs`.
Errores de compilación CS0104 y CS1503.

**Fix:**
Calificación completa del tipo en todos los handlers `@onchange`:

```csharp
// ❌ Antes
private async Task OnFavLocationChanged(ChangeEventArgs e)

// ✅ Después
private async Task OnFavLocationChanged(Microsoft.AspNetCore.Components.ChangeEventArgs e)
```

---

## Archivos modificados / creados

| Acción | Archivo |
|--------|---------|
| 🆕 Creado | `ObservApp.Shared/Pages/CalculadoraSolLuna.razor` |
| 🆕 Creado | `ObservApp.Shared/Pages/CalculadoraSolLuna.razor.css` |
| 🆕 Creado | `ObservApp.Shared/Pages/GestionUbicaciones.razor` |
| 🆕 Creado | `ObservApp.Shared/Pages/GestionUbicaciones.razor.css` |
| 🆕 Creado | `ObservApp.Shared/Models/FavoriteLocation.cs` |
| 🆕 Creado | `ObservApp.Shared/Services/IGeolocationService.cs` |
| 🆕 Creado | `ObservApp.Shared/Services/IFavoriteLocationsService.cs` |
| 🆕 Creado | `ObservApp.Shared/wwwroot/geolocation.js` |
| 🆕 Creado | `ObservApp.Shared/wwwroot/leaflet-map.js` |
| 🆕 Creado | `ObservApp/Services/MauiGeolocationService.cs` |
| 🆕 Creado | `ObservApp/Services/MauiFavoriteLocationsService.cs` |
| 🆕 Creado | `ObservApp.Web.Client/Services/WebGeolocationService.cs` |
| 🆕 Creado | `ObservApp.Web.Client/Services/WebFavoriteLocationsService.cs` |
| 🆕 Creado | `ObservApp.Web/Services/SsrGeolocationService.cs` |
| 🆕 Creado | `ObservApp.Web/Services/SsrFavoriteLocationsService.cs` |
| ✏️ Modificado | `ObservApp.Shared/Pages/Calculadoras.razor` |
| ✏️ Modificado | `ObservApp.Shared/Pages/Configuracion.razor` |
| ✏️ Modificado | `ObservApp.Shared/Resources/Strings/App.*.resx` (×6 idiomas) |
| ✏️ Modificado | `ObservApp/MauiProgram.cs` |
| ✏️ Modificado | `ObservApp/wwwroot/index.html` |
| ✏️ Modificado | `ObservApp/Platforms/Android/AndroidManifest.xml` |
| ✏️ Modificado | `ObservApp/Platforms/iOS/Info.plist` |
| ✏️ Modificado | `ObservApp.Web/Program.cs` |
| ✏️ Modificado | `ObservApp.Web.Client/Program.cs` |
| ✏️ Modificado | `ObservApp.Web/Components/App.razor` |
| ✏️ Modificado | `ObservApp.Web.Client/Services/WebGeolocationService.cs` (fix CancellationToken) |
