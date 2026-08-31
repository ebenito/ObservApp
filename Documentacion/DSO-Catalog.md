# 🌌 Catálogo DSO (Deep Sky Objects)

## Descripción

El catálogo DSO de ObservApp contiene información sobre **objetos de cielo profundo** (galaxias, nebulosas, cúmulos, etc.) que pueden observarse desde cualquier ubicación en la Tierra.

- **Ubicación del archivo:** `ObservApp.Shared/Resources/dso-catalog.json`
- **Formato:** JSON embebido en el ensamblado
- **Tamaño:** Datos optimizados para descargas web y aplicaciones móviles
- **Actualización:** Puedes actualizar el catálogo sustituyendo el archivo

---

## Estructura del archivo

### Formato JSON

```json
{
  "catalog": [
	{
	  "id": "M001",
	  "name": "M1 (Nebulosa del Cangrejo)",
	  "commonNames": ["Crab Nebula", "NGC 1952"],
	  "type": "Nebulosa",
	  "constellation": "Tauro",
	  "magnitude": 8.4,
	  "size": {
		"width": 6.0,
		"height": 4.0
	  },
	  "coordinates": {
		"ra": "05h34m31s",
		"dec": "+22°00'52\"",
		"raHours": 5.5753,
		"decDegrees": 22.0142
	  },
	  "description": "Remanente de supernova histórica de 1054",
	  "observingDifficulty": "Fácil",
	  "bestSeason": "Invierno",
	  "imageUrl": null
	},
	...
  ]
}
```

### Campos principales

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | string | Identificador único (ej: "M001", "NGC1234") |
| `name` | string | Nombre principal (ej: "M1 (Nebulosa del Cangrejo)") |
| `commonNames` | array | Denominaciones alternativas |
| `type` | string | Tipo: "Nebulosa", "Galaxia", "Cúmulo Globular", etc. |
| `constellation` | string | Constelación donde se encuentra |
| `magnitude` | number | Magnitud aparente (menor = más brillante) |
| `size` | object | Dimensiones en arcominutos {width, height} |
| `coordinates.ra` | string | Ascensión recta (formato: "HHhMMmSSs") |
| `coordinates.dec` | string | Declinación (formato: "±DD°MM'SS"") |
| `coordinates.raHours` | number | RA en horas decimales (0-24) |
| `coordinates.decDegrees` | number | Declinación en grados decimales (-90 a +90) |
| `description` | string | Información observacional y características |
| `observingDifficulty` | string | "Fácil", "Moderada", "Difícil" |
| `bestSeason` | string | Época óptima de observación |
| `imageUrl` | string/null | URL a imagen de referencia (opcional) |

---

## Acceso desde código

### Inyección del proveedor

```csharp
@inject IDsoCatalogProvider DsoCatalogProvider

@code {
	private List<DsoObject> todosLosObjetos;

	protected override async Task OnInitializedAsync()
	{
		// Cargar todo el catálogo
		todosLosObjetos = await DsoCatalogProvider.GetAllObjectsAsync();
	}
}
```

### Búsqueda y filtrado

```csharp
// Buscar por nombre
var m1 = todosLosObjetos
	.FirstOrDefault(obj => obj.Name.Contains("M1"));

// Filtrar por tipo
var nebulosas = todosLosObjetos
	.Where(obj => obj.Type == "Nebulosa")
	.ToList();

// Filtrar por magnitud (objetos brillantes)
var objetosBrillantes = todosLosObjetos
	.Where(obj => obj.Magnitude <= 10)
	.OrderBy(obj => obj.Magnitude)
	.ToList();

// Filtrar por constelación
var enOrion = todosLosObjetos
	.Where(obj => obj.Constellation == "Orión")
	.ToList();

// Filtrar por dificultad de observación
var faciles = todosLosObjetos
	.Where(obj => obj.ObservingDifficulty == "Fácil")
	.ToList();
```

### Búsqueda geométrica (por coordenadas)

```csharp
// Encontrar objetos visibles en una ubicación/fecha
// (usualmente combinado con cálculos de posición del cielo)

var ra = 5.5753; // ascensión recta en horas
var dec = 22.0142; // declinación en grados

// Objetos dentro de un radio angular (ej: 5°)
var radioAngular = 5.0;
var objetosCercanos = todosLosObjetos
	.Where(obj => 
	{
		var distancia = CalcularDistanciaAngular(
			ra, dec,
			obj.Coordinates.RaHours, obj.Coordinates.DecDegrees
		);
		return distancia <= radioAngular;
	})
	.ToList();
```

---

## Tipos de objetos soportados

| Tipo | Símbolo | Descripción |
|---|---|---|
| **Galaxia** | `🌀` | Sistemas estelares lejanos |
| **Nebulosa** | `☁️` | Nubes de gas y polvo |
| **Nebulosa de emisión** | `🔴` | Gas ionizado que emite luz |
| **Nebulosa oscura** | `⬛` | Polvo que bloquea luz |
| **Nebulosa planetaria** | `💫` | Capa de gas expelida por estrella moribunda |
| **Cúmulo globular** | `●●●` | Agrupación esférica de estrellas |
| **Cúmulo abierto** | `* * *` | Agrupación dispersa de estrellas |
| **Supernova** | `✦` | Remanente de explosión estelar |

---

## Actualizar el catálogo

### Desde archivo externo (CSV, JSON)

1. Prepara un archivo con los datos en formato compatible
2. Reemplaza `ObservApp.Shared/Resources/dso-catalog.json`
3. Recompila la solución

### Estructura mínima requerida

Para que un objeto sea válido, necesita al menos:

```json
{
  "id": "IDENTIFICADOR",
  "name": "NOMBRE",
  "type": "TIPO",
  "coordinates": {
	"raHours": 0.0,
	"decDegrees": 0.0
  },
  "magnitude": 0.0
}
```

---

## Rendimiento y optimización

### Carga del catálogo

- ✅ **Cargado una sola vez** en el ciclo de inicialización
- ✅ **En caché** en memoria para acceso rápido
- ✅ **Sincróno** (no requiere I/O de red)

### Búsquedas complejas

Para búsquedas complejas o frecuentes, considera:

```csharp
// Crear índices en memoria
var indicesPorTipo = todosLosObjetos
	.GroupBy(obj => obj.Type)
	.ToDictionary(g => g.Key, g => g.ToList());

var indicesPorConstacion = todosLosObjetos
	.GroupBy(obj => obj.Constellation)
	.ToDictionary(g => g.Key, g => g.ToList());

// Búsquedas posteriores son O(1)
var galaxias = indicesPorTipo["Galaxia"];
```

---

## Ejemplos de uso en Efemérides

### Mostrar objetos visibles en la noche

```razor
@if (_objetosVisibles != null)
{
	<div class="dso-list">
		@foreach (var obj in _objetosVisibles.OrderBy(x => x.Magnitude))
		{
			<div class="dso-item">
				<strong>@obj.Name</strong>
				<span>@obj.Type — Mag: @obj.Magnitude.ToString("F1")</span>
				<p>@obj.Description</p>
			</div>
		}
	</div>
}
```

### Integración con posición celeste calculada

```csharp
// Dados datos astronómicos calculados (ej: posición de Sirio)
var posicionCalculada = new PosicionCeleste
{
	RaHoras = 6.7525,
	DecGrados = -16.7161
};

// Encontrar objetos cercanos en el cielo
var objetosCercanos = ObtenerObjetosCercanos(
	posicionCalculada.RaHoras,
	posicionCalculada.DecGrados,
	radioAngular: 5.0 // 5 grados
);
```

---

## Licencia y atribución

El catálogo DSO de ObservApp utiliza datos de:
- **NASA Messier Catalog**
- **NGC (New General Catalog)**
- **Hipparcos-2 Catalog**
- **Observatorios astronómicos públicos**

Todos los datos son de dominio público o bajo licencias abiertas (Creative Commons).

---

## Notas para desarrolladores

### Agregar nuevos objetos

1. Edita `dso-catalog.json`
2. Agrega entrada con estructura válida
3. Valida JSON (usa [jsonlint.com](https://jsonlint.com))
4. Recompila: `dotnet build`

### Testing

```csharp
[TestClass]
public class DsoCatalogTests
{
	[TestMethod]
	public async Task CatalogCanBeLoaded()
	{
		var provider = new DsoCatalogProvider();
		var catalog = await provider.GetAllObjectsAsync();

		Assert.IsNotNull(catalog);
		Assert.IsTrue(catalog.Count > 0);
	}

	[TestMethod]
	public async Task AllObjectsHaveRequiredFields()
	{
		var catalog = await provider.GetAllObjectsAsync();

		foreach (var obj in catalog)
		{
			Assert.IsFalse(string.IsNullOrEmpty(obj.Id));
			Assert.IsFalse(string.IsNullOrEmpty(obj.Name));
			Assert.IsTrue(obj.Coordinates.RaHours >= 0 && obj.Coordinates.RaHours <= 24);
			Assert.IsTrue(obj.Coordinates.DecDegrees >= -90 && obj.Coordinates.DecDegrees <= 90);
		}
	}
}
```

---

## Referencias

- [CosineKitty Astronomy Engine](https://www.cosinekitty.com/cosmos/astronomy_engine.html) — Cálculos de posiciones
- [Messier Catalog](https://www.messier.seds.org/) — Objetos Messier
- [NGC Catalog](http://www.ngcicproject.org/) — NGC e IC objects
- [International Astronomical Union](https://www.iau.org/) — Estándares astronómicos
