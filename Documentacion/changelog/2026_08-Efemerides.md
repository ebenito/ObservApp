# Efemerides — documentación de la página y arquitectura actual

## 1. Objetivo de la página

`ObservApp.Shared/Pages/Efemerides.razor` es la pantalla principal de efemérides astronómicas de ObservApp. Su propósito es ofrecer una vista unificada de:

- fecha y ubicación del observador,
- próximo eclipse visible localmente,
- planetas del sistema solar para la fecha seleccionada,
- eventos astronómicos relevantes,
- condiciones de observación de la noche,
- catálogo de objetos de cielo profundo (DSO) con filtros, búsqueda y resultados ordenados por altitud máxima.

La página se comporta como “orquestador” del estado y del cálculo. La lógica de negocio y el cálculo astronómico se mantienen en el componente padre y en servicios inyectados, mientras que parte del marcado visual se ha extraído a subcomponentes de presentación.

## 2. Ruta y acceso

- Ruta Blazor: `/efemerides`
- Componente principal: `ObservApp.Shared/Pages/Efemerides.razor`
- Documento de estilos aislados: `ObservApp.Shared/Pages/Efemerides.razor.css`

## 3. Componentes actuales implicados

La página está compuesta por una mezcla de markup inline y subcomponentes presentacionales. En la versión actual del workspace se usan estos elementos principales:

### 3.1. `Efemerides.razor`
Es el componente raíz y la fuente de verdad del estado. Define:

- `@inject IStringLocalizer<ObservApp.Resources.Strings.App> L`
- `@inject IEfemeridesAstronomyService EphAstronomyService`
- `@inject IDsoCatalogProvider DsoCatalogProvider`
- `@inject IFilterSvgIconProvider SvgIcons`
- `@inject EfemeridesViewModel VM`
- `@inject IJSRuntime JS`
- `@inject ILogger<Efemerides> Logger`

Su responsabilidad es:

- mantener la fecha, latitud, longitud y altitud del observador,
- reaccionar a cambios del `EfemeridesViewModel`,
- lanzar los cálculos astronómicos con `CalcularAsync()` y `CalcularDsosAsync()`,
- filtrar y mostrar la lista DSO,
- gestionar referencias a los 4 `SfComboBox` para la UI tipo selector de DSO,
- ejecutar JS interop para habilitar el comportamiento de los combos.

### 3.2. `EclipseBanner.razor`
Subcomponente de presentación para el banner del próximo eclipse visible. Recibe:

- `EclipseDefinition? NextEclipse`
- `LocalEclipseResult? NextEclipseResult`

Renderiza el bloque visual solo si ambos valores existen. Mantiene la lógica de presentación, pero no calcula nada.

### 3.3. `PlanetsSection.razor`
Subcomponente de presentación para la sección de planetas. Recibe:

- `List<PlanetData> Planetas`

Renderiza la tabla grande y la vista móvil en tarjetas con los estilos `eph-planets-table`, `eph-planets-cards` y `stat-row`.

### 3.4. `EventsSection.razor`
Subcomponente de presentación para la lista de eventos astronómicos. Recibe:

- `List<EphEvent> Eventos`

Se encarga del bloque `eph-events-list` sin lógica de cálculo.

### 3.5. `TonightSection.razor`
Subcomponente de presentación para “Cielo esta noche”. Recibe:

- `TonightData? Tonight`

Muestra la calidad de la noche, fase lunar, iluminación, inicio del crepúsculo astronómico y salidas/puestas de la Luna.

### 3.6. `DsoFilterItem.razor`
Componente visual para cada opción dentro de los filtros DSO. Recibe:

- `AvatarClass`
- `Label`
- `Subtitle`
- `SvgContent`

Tiene la misión de mantener el mismo DOM visual que el antiguo `RenderFragment` construido con `RenderTreeBuilder`, para no romper el CSS aislado de `Efemerides.razor.css`.

## 4. Arquitectura de datos y modelos

La página se apoya en modelos de datos y en un ViewModel dedicado.

### 4.1. `EfemeridesViewModel`
Ubicación: `ObservApp.Shared/ViewModels/EfemeridesViewModel.cs`

Responsabilidades:

- mantener el estado compartido de la ubicación y fecha,
- sincronizar datos con `ILocationStateService`,
- manejar favoritos y localización GPS,
- notificar cambios con `OnStateChanged` para recálculo de la página.

Propiedades relevantes:

- `Fecha`
- `Latitude`
- `Longitude`
- `AltitudeMeters`
- `FavoriteLocations`
- `GpsLoading`
- `GpsError`
- `GpsSource`

### 4.2. `PlanetData`, `EphEvent`, `TonightData`, `DsoEntry`, `DsoVisibility`
Ubicación: `ObservApp.Shared/Models/PageModels/EfemeridesPageModels.cs`

Estas clases moldean la salida del cálculo astronómico y de DSO:

- `PlanetData`: nombre, emoji, altitud máxima, acimut, salida/puesta, distancia, iluminación.
- `EphEvent`: evento, fecha y descripción.
- `TonightData`: calidad, fase lunar, iluminación, hora astronómica, salida/puesta lunar.
- `DsoEntry`: identificador, nombre, constelación, tipo, magnitud, catálogo, aliases, requisitos de instrumento/sky.
- `DsoVisibility`: resultado de visibilidad del DSO para la noche del día seleccionado.

### 4.3. `DsoCatalogProvider`
Ubicación: `ObservApp.Shared/Services/DsoCatalogProvider.cs`

Es un proveedor de catálogo DSO que carga el contenido desde un JSON embebido en recursos del ensamblado:

- recurso: `Resources.dso-catalog.json`
- estrategia: `Lazy<IReadOnlyList<DsoEntry>>`
- serialización con `System.Text.Json`

Esto permite separar el catálogo de datos del código y mantenerlo como recurso embebido en lugar de hardcodearlo en la página.

## 5. Servicios y dependencias principales

### 5.1. `IEfemeridesAstronomyService`
Se encarga de calcular:

- planetas visibles,
- eventos astronómicos del día/periodo,
- noche astronómica y lunar,
- visibilidad DSO por noche.

La implementación actual es `EfemeridesAstronomyService` y usa `CosineKitty` para cálculos astronómicos y `Observer` con altitud en km.

### 5.2. `IEclipseCalculatorService`
Responsable de los eclipses globales y locales.

- `GetUpcomingEclipses(int years = 6)`
- `Calculate(...)` para una ubicación concreta

Esto alimenta el banner local y la detección del próximo eclipse visible.

### 5.3. `IFilterSvgIconProvider`
Ubicación:

- `ObservApp.Shared/Services/IFilterSvgIconProvider.cs`
- `ObservApp.Shared/Services/FilterSvgIconProvider.cs`

Es la abstracción para los iconos SVG de los filtros DSO. El proveedor contiene un diccionario estático con todos los SVGs de:

- catálogo,
- constelación,
- instrumento,
- cielo.

Incluye fallback a `catalog-all` cuando no existe la clave.

## 6. Flujo de cálculo de la página

El flujo de la página se mantiene en `Efemerides.razor` del modo siguiente:

### 6.1. Inicialización
Durante `OnInitializedAsync()`:

- se suscribe a `VM.OnStateChanged`,
- se inicializa `VM.InitializeAsync()`,
- se invoca `CalcularAsync()` para producir la primer vista.

### 6.2. `CalcularAsync()`
Este método es el orquestador del cálculo principal. Tiene la siguiente lógica:

- actualiza el estado local a partir del ViewModel,
- usa `Task.Run` para generar el cálculo en segundo plano,
- mantiene un contador `_calcGeneration` para descartar cálculos obsoletos,
- obtiene datos astronómicos usando `EphAstronomyService.Compute(...)`,
- obtiene el próximo eclipse visible local con `FindNextVisibleEclipse(...)`,
- repinta la UI con `StateHasChanged()`.

Pauta de seguridad:

- si entra un cálculo nuevo, el cálculo anterior queda invalidado al comparar una generación,
- la lógica evita condiciones de carrera al calcular priorizando la última petición.

### 6.3. `CalcularDsosAsync()`
Se ejecuta con su propio `CancellationTokenSource` (`_calcCts`) y usa una validación `IsCurrentCalc(token)` para evitar que un cálculo anterior reescriba el resultado final.

Se encarga de:

- ordenar los DSO por visibilidad y brillo,
- filtrar por los criterios del usuario,
- calcular la ventana astronómica de la noche,
- construir `DsoVisibility` para la lista final.

## 7. Filtros y búsqueda DSO

La sección DSO tiene controles en el mismo componente raíz:

- buscador de texto (`_dsoFilterSearch`),
- 4 `SfComboBox`:
  - catálogo,
  - constelación,
  - instrumento,
  - cielo,
- `DsoFiltered` como agregado de filtros sobre `_dsoVisibles`.

Los filtros funcionan sobre:

- `GetDsoCatalogPrefix(item.Dso)`
- `item.Dso.Constellation`
- `GetDsoInstrumentCategory(item.Dso.RecommendedInstrument)`
- `item.Dso.SkyRequirement`

La búsqueda admite coincidencias por:

- id del objeto,
- nombre,
- alias,
- catálogo.

## 8. JS interop y Syncfusion

La sección de DSO usa `Syncfusion.Blazor.DropDowns` y mantiene referencias a los combos:

- `_catalogCombo`
- `_constellationCombo`
- `_instrumentCombo`
- `_skyCombo`

En `OnAfterRenderAsync(bool firstRender)`, si es el primer render, llama a:

`JS.InvokeVoidAsync("setupComboBoxClickHandlers", new[] { "eph-dso-catalog", ... })`

Esto se hace para que el popup de cada `SfComboBox` abra con el comportamiento visual y clic esperado. El enfoque actual es mantener esta parte en el padre para evitar romper la interop con las referencias y los `@bind-Value` de cada combo.

## 9. CSS e isolación

El estilo de la página vive en:

- `ObservApp.Shared/Pages/Efemerides.razor.css`

La estrategia actual es la siguiente:

- el CSS de la página aplica a `Efemerides.razor` por isolation normal,
- los subcomponentes que se extraen a otros archivos conservan los estilos a través de selectores `::deep` para mantener el árbol visual sin duplicar hojas `.razor.css` en cada hijo,
- se mantienen las clases `eph-*` para no cambiar el DOM ni romper el CSS existente.

Los elementos con estilos específicos incluyen:

- banner de eclipse: `eph-eclipse-banner*`
- tabla y tarjetas planetarias: `eph-planets-table*`, `eph-planets-cards`
- DSO: `eph-dso-*`, `eph-dso-combo`, `eph-dso-card`, `eph-dso-track`

## 10. Registro de dependencias

El host web y MAUI registran las dependencias necesarias para la pantalla.

### 10.1. `ObservApp.Web/Program.cs`
Registra:

- `IFilterSvgIconProvider`
- `IDsoCatalogProvider`
- `IEfemeridesAstronomyService`
- `IEclipseCalculatorService`
- `EfemeridesViewModel`

### 10.2. `ObservApp.Web.Client/Program.cs`
Registra igualmente la dependencia del proveedor SVG para que la vista funcione en WASM.

### 10.3. `ObservApp/MauiProgram.cs`
Registra la misma dependencia para la versión MAUI.

## 11. Estado actual del refactor visual

La página ya ha recibido una división parcial en subcomponentes de presentación:

- `EclipseBanner.razor`
- `PlanetsSection.razor`
- `EventsSection.razor`
- `TonightSection.razor`
- `DsoFilterItem.razor`

Sin embargo, la sección DSO principal sigue siendo un bloque del componente padre para evitar romper los `@ref` y el JS interop de Syncfusion.

Esto mantiene el equilibrio entre:

- lógica de cálculo y estado en el padre,
- presentación en subcomponentes,
- estabilidad del CSS y del comportamiento de Syncfusion.

## 12. Observaciones de diseño

- La página se mantiene como una “página de agregación” con lógica de cálculo centralizada; no se duplica la lógica astronómica en los subcomponentes.
- El cálculo de visibilidad y filtros se maneja en el padre para conservar una sola fuente de verdad.
- Los subcomponentes son “presentacionales” y reciben datos ya preparados.
- El flujo de datos evita que los secundarios recalculen decisiones de negocio.

## 13. Resumen arquitectónico

La arquitectura actual de la pantalla es esta:

- UI Blazor (`Efemerides.razor`)
  - mantiene estado y orquestación,
  - calcula y filtra,
  - controla JS interop de Syncfusion,
  - renderiza subcomponentes visuales.
- ViewModel (`EfemeridesViewModel`)
  - expone ubicación, fecha y GPS,
  - centraliza cambios del estado global.
- Servicios (`EfemeridesAstronomyService`, `IEclipseCalculatorService`, `DsoCatalogProvider`)
  - encapsulan astronomía y catálogo.
- Modelos (`EfemeridesPageModels`)
  - representan las entidades y resultados de cálculo.
- Styling (`Efemerides.razor.css` + `::deep` en hijos seleccionados)
  - asegura compatibilidad total con el estilo existente.

## 14. Punto de entrada recomendado para continuidad

Si se quiere seguir ampliando la refactorización, el siguiente punto lógico es la sección DSO completa, pero manteniendo el patrón actual de `Efemerides.razor` como propietario de:

- los `SfComboBox`,
- los `@bind-Value`,
- el `OnAfterRenderAsync` con JS,
- la lógica de filtrado y recuperación del listado.

Esto minimiza el riesgo de regresión y preserva el CSS actual sin reescribir `Efemerides.razor.css`.
