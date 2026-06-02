# Plan de Implementación — Gestión de Ubocaciones Favoritas (Actualizado)

Este plan detalla el diseño e implementación de un sistema de gestión de ubicaciones favoritas para **ObservApp**, permitiendo al usuario guardar ubicaciones (vía GPS o buscando en un mapa interactivo con un buscador de Nominatim) y cargarlas en las diferentes calculadoras astronómicas de la aplicación.

---

## Decisiones de Arquitectura y Diseño

### 1. Leaflet.js con OSM vs. Syncfusion Blazor Maps
Aunque Syncfusion está instalado en el proyecto (`Syncfusion.Blazor.Maps`), se ha optado por utilizar **Leaflet.js** con mapas base de **OpenStreetMap** (OSM) por las siguientes razones de peso:
* **Marcador Arrastrable Fluido**: En Leaflet, hacer un marcador arrastrable es nativo y sumamente fluido (`{ draggable: true }`). En Syncfusion Maps, arrastrar marcadores en tiempo real requiere capturar eventos del ratón/táctiles de bajo nivel en Blazor y recalcular coordenadas, lo cual genera retrasos (lag) notables en WebAssembly y dispositivos móviles.
* **Peso y Rendimiento**: Syncfusion Maps es un control pesado enfocado en visualización de datos estadísticos (coropletas) y mapas vectoriales. Para cargar un mapa callejero dinámico interactivo, Leaflet pesa menos de 150 KB (JS+CSS) y tiene un rendimiento insuperable en dispositivos móviles con soporte táctil nativo.
* **Control de Vista**: Leaflet permite centrar y realizar animaciones de zoom de forma muy sencilla (`flyTo` o `setView`), mientras que en Syncfusion manipular el zoom dinámico por código requiere sincronizar estados complejos de enlace de datos que a veces generan inconsistencias visuales.

### 2. Consulta Automática de Altitud (Elevación)
Para evitar que la altitud sea siempre 0 por defecto al buscar o seleccionar un lugar, integraremos la API de elevación pública y gratuita de **Open-Meteo**:
* **Flujo**: Cuando el usuario busque un lugar o arrastre la chincheta del mapa, tras un pequeño retraso de cortesía (debounce) para no saturar, realizaremos una petición HTTP asíncrona a:
  `https://api.open-meteo.com/v1/elevation?latitude={lat}&longitude={lon}`
* **Fallback**: La respuesta nos dará la elevación en metros sobre el nivel del mar con alta precisión y la asignaremos al input del formulario de forma automática. Dejaremos que el usuario aún pueda editarla a mano por si está offline o quiere afinarla.

3. **Abstracción Multiplataforma**:
   Definiremos la interfaz `IFavoriteLocationsService` en el proyecto compartido `ObservApp.Shared` e implementaremos la persistencia de forma independiente según la plataforma:
   * **MAUI**: `MauiFavoriteLocationsService` usará `Microsoft.Maui.Storage.Preferences` serializando la lista en un JSON.
   * **Web WASM**: `WebFavoriteLocationsService` usará `localStorage` mediante interoperabilidad de JS (`IJSRuntime`).
   * **Web SSR**: `SsrFavoriteLocationsService` como fallback inerte para el renderizado inicial de ASP.NET Core.

---

## Preguntas abiertas / Requiere revisión

> [!NOTE]
> 1. **¿Persistencia en la nube en el futuro?**
>    Toda la UI consumirá la interfaz `IFavoriteLocationsService`. Cuando implementemos **Supabase**, podremos sustituir el almacenamiento local (Preferences / localStorage) por persistencia en la base de datos de la nube simplemente creando una nueva implementación de la interfaz, sin tener que modificar una sola línea de código en las pantallas de Blazor.

---

## Cambios Propuestos

### Componente Shared (Lógica y UI Comunes)

---

#### [NEW] [FavoriteLocation.cs](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/Models/FavoriteLocation.cs)
Modelo de datos para almacenar una ubicación.
```csharp
namespace ObservApp.Shared.Models;

public class FavoriteLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AltitudeMeters { get; set; }
}
```

#### [NEW] [IFavoriteLocationsService.cs](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/Services/IFavoriteLocationsService.cs)
Interfaz del servicio de persistencia de ubicaciones.
```csharp
using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

public interface IFavoriteLocationsService
{
    Task<List<FavoriteLocation>> GetFavoriteLocationsAsync();
    Task SaveFavoriteLocationAsync(FavoriteLocation location);
    Task DeleteFavoriteLocationAsync(Guid id);
}
```

#### [NEW] [leaflet-map.js](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/wwwroot/leaflet-map.js)
Código JavaScript para inicializar el mapa Leaflet y reportar a Blazor la nueva posición de la chincheta tras arrastrarla.
```javascript
window.observApp = window.observApp || {};

window.observApp.initMap = function (containerId, lat, lon, zoom, dotNetRef) {
    var container = document.getElementById(containerId);
    if (!container) return false;

    if (container._leaflet_map) {
        container._leaflet_map.remove();
    }

    var map = L.map(containerId).setView([lat, lon], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    var marker = L.marker([lat, lon], { draggable: true }).addTo(map);

    marker.on('dragend', function (event) {
        var markerPos = event.target.getLatLng();
        dotNetRef.invokeMethodAsync('OnMarkerDragged', markerPos.lat, markerPos.lng);
    });

    container._leaflet_map = map;
    container._leaflet_marker = marker;
    return true;
};

window.observApp.updateMapPosition = function (containerId, lat, lon, zoom) {
    var container = document.getElementById(containerId);
    if (container && container._leaflet_map && container._leaflet_marker) {
        var newPos = [lat, lon];
        container._leaflet_map.setView(newPos, zoom);
        container._leaflet_marker.setLatLng(newPos);
    }
};
```

#### [NEW] [GestionUbicaciones.razor](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/Pages/GestionUbicaciones.razor)
Página para agregar, editar y eliminar ubicaciones favoritas. Contará con:
- Botón para guardar ubicación actual obtenida de `GeoService`.
- Buscador conectado con la API de Nominatim.
- Div `#map` que representará el mapa interactivo llamando a Leaflet.js mediante `IJSRuntime`.
- Petición asíncrona a la API de **Open-Meteo Elevation** al actualizar las coordenadas (para rellenar la altitud automáticamente).
- Inputs de texto para configurar el nombre personalizado y corregir la altitud a mano si el usuario lo desea.

#### [NEW] [GestionUbicaciones.razor.css](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/Pages/GestionUbicaciones.razor.css)
Estilos CSS específicos de la vista, incluyendo el tamaño y aspecto del mapa en dispositivos móviles y de escritorio.

#### [MODIFY] [Configuracion.razor](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/Pages/Configuracion.razor)
Se añadirá una sección debajo de la selección de idioma:
* Título: **Ubicaciones Guardadas**
* Botón/Tarjeta: **Gestionar ubicaciones favoritas 🗺️** (enlace a `/configuracion/ubicaciones`).

#### [MODIFY] [CalculadoraSolLuna.razor](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/Pages/CalculadoraSolLuna.razor)
* Carga asíncrona de favoritos al inicializar.
* Agregar un selector desplegable en la sección de ubicación para elegir entre "Manual", "GPS Actual" o una de las ubicaciones favoritas guardadas.
* Actualizar y recalcular datos automáticamente al elegir cualquiera.

#### [MODIFY] [App.en.resx](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/Resources/Strings/App.en.resx) y [App.es.resx](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Shared/Resources/Strings/App.es.resx)
Agregar las traducciones de las nuevas interfaces (ej. "Settings_FavoriteLocations", "FavLoc_Title", "FavLoc_Search_Placeholder", etc.).

---

### Proyecto Host MAUI (`ObservApp`)

---

#### [NEW] [MauiFavoriteLocationsService.cs](file:///c:/Proyectos/XTV/ObservApp/ObservApp/Services/MauiFavoriteLocationsService.cs)
Implementa `IFavoriteLocationsService` utilizando `Microsoft.Maui.Storage.Preferences`.

#### [MODIFY] [MauiProgram.cs](file:///c:/Proyectos/XTV/ObservApp/ObservApp/MauiProgram.cs)
* Registrar `builder.Services.AddSingleton<IFavoriteLocationsService, MauiFavoriteLocationsService>();`

#### [MODIFY] [index.html](file:///c:/Proyectos/XTV/ObservApp/ObservApp/wwwroot/index.html)
* Añadir scripts y estilos CSS de Leaflet (vía CDN).
* Añadir `<script src="_content/ObservApp.Shared/leaflet-map.js"></script>`.

---

### Proyecto Host Web (`ObservApp.Web` y `ObservApp.Web.Client`)

---

#### [NEW] [WebFavoriteLocationsService.cs](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Web.Client/Services/WebFavoriteLocationsService.cs)
Implementa `IFavoriteLocationsService` en WebAssembly utilizando `localStorage` vía JSInterop.

#### [NEW] [SsrFavoriteLocationsService.cs](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Web/Services/SsrFavoriteLocationsService.cs)
Implementación dummy que retorna una lista vacía para evitar errores de renderizado de lado servidor.

#### [MODIFY] [Program.cs](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Web/Program.cs)
* Registrar `builder.Services.AddSingleton<IFavoriteLocationsService, SsrFavoriteLocationsService>();`

#### [MODIFY] [Program.cs](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Web.Client/Program.cs)
* Registrar `builder.Services.AddSingleton<IFavoriteLocationsService, WebFavoriteLocationsService>();`

#### [MODIFY] [App.razor](file:///c:/Proyectos/XTV/ObservApp/ObservApp.Web/Components/App.razor)
* Añadir estilos CSS y scripts JS de Leaflet (vía CDN).
* Añadir `<script src="_content/ObservApp.Shared/leaflet-map.js"></script>`.

---

## Plan de Verificación

### Pruebas Manuales
1. **Verificación de UI en Configuración**:
   * Navegar a la sección de Configuración y validar que aparece el botón para gestionar ubicaciones favoritas.
   * Hacer clic y verificar que navega a `/configuracion/ubicaciones` correctamente.
2. **Prueba de Mapa, Buscador y Altitud**:
   * Validar que el mapa Leaflet se inicializa y carga correctamente en la pantalla de gestión de ubicaciones.
   * Buscar un lugar en el buscador de Nominatim (ej. "Tenerife") y comprobar que el mapa se centra allí y pone la chincheta.
   * Verificar que al ubicar el marcador, se llama a la API de Open-Meteo y la altitud se actualiza automáticamente con la elevación real del lugar (ej. si buscamos el pico del Teide, debe marcar cerca de 3718 metros).
   * Probar el arrastre manual de la chincheta y comprobar que actualiza latitud, longitud y altitud tras soltarla.
3. **Prueba de Persistencia**:
   * Añadir una ubicación favorita, recargar la aplicación (o reiniciar en MAUI) y confirmar que la ubicación sigue existiendo en el listado.
4. **Prueba en Calculadora Sol-Luna**:
   * Cargar la calculadora Sol-Luna, seleccionar una ubicación favorita del desplegable y validar que los cálculos solares y lunares se actualizan con las coordenadas y altitud del favorito seleccionado.
