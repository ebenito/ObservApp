# Navegación Dinámica: Botones de Volver Contextuales

## Problema Resuelto
Los botones de "Volver" en las páginas estaban hardcodeados a URLs específicas (como `/calculadoras`), en lugar de volver dinámicamente a la página que había llamado.

## Solución Implementada

### 1. **Servicio de Historial de Navegación** (`INavigationHistoryService`)
- **Archivo**: `ObservApp.Shared\Services\INavigationHistoryService.cs` (interfaz)
- **Archivo**: `ObservApp.Shared\Services\NavigationHistoryService.cs` (implementación)
- **Responsabilidad**: Mantener un stack (pila) de URLs visitadas
- **Métodos principales**:
  - `PushUrl(url)`: Registra una URL en el historial
  - `PopUrl(defaultUrl)`: Obtiene la URL anterior y elimina la entrada actual
  - `Clear()`: Limpia el historial
  - `Count`: Número de URLs en el historial

### 2. **Componente BackButton** 
- **Archivo**: `ObservApp.Shared\Components\BackButton.razor`
- **Parámetros**:
  - `Text`: Clave de localización del texto del botón (ej: "Action_Back")
  - `CssClass`: Clase CSS para estilos (ej: "soluna-back", "fov-back")
  - `DefaultUrl`: URL por defecto si no hay historial (ej: "/calculadoras")
- **Funcionalidad**: Recupera la URL anterior del servicio y navega a ella

### 3. **Rastreo Automático en MainLayout**
- **Archivo modificado**: `ObservApp.Shared\Layout\MainLayout.razor`
- **Cambios**:
  - Se inyecta `INavigationHistoryService`
  - En `OnInitialized()`: Se registra la URL inicial
  - En `OnLocationChanged()`: Se registra cada cambio de navegación automáticamente
  - El registro es transparente para el usuario

### 4. **Páginas Actualizadas**
Se reemplazaron los enlaces hardcodeados con el componente `BackButton`:

| Página | Clase CSS | DefaultUrl |
|--------|-----------|-----------|
| CalculadoraSolLuna.razor | `soluna-back` | `/calculadoras` |
| CalculadoraFOV.razor | `fov-back` | `/calculadoras` |
| CalculadoraEclipse.razor | `eclipse-back` | `/calculadoras` |
| CalculadoraTiemposEclipse.razor | `eclt-back` | `/calculadoras` |

### 5. **Registro en Inyección de Dependencias**
Se agregó `INavigationHistoryService` como servicio inyectable en:
- **ObservApp.Web\Program.cs** (SSR): `AddScoped<INavigationHistoryService, NavigationHistoryService>()`
- **ObservApp.Web.Client\Program.cs** (WASM): `AddSingleton<INavigationHistoryService, NavigationHistoryService>()`
- **ObservApp\MauiProgram.cs** (MAUI): `AddSingleton<INavigationHistoryService, NavigationHistoryService>()`

## Flujo de Funcionamiento

1. **Navegación inicial**: Cuando el usuario entra al sitio, `MainLayout.OnInitialized()` registra la URL actual
2. **Navegación posterior**: Cada vez que el usuario navega, `OnLocationChanged()` registra la nueva URL
3. **Botón de volver**: Cuando hace clic en "Volver", el `BackButton` obtiene la URL anterior del stack y la elimina
4. **Fallback**: Si el historial está vacío, usa el `DefaultUrl` especificado (ej: `/calculadoras`)

## Ventajas

✅ **Flexible**: Los botones pueden volver a cualquier página anterior, no solo a una predefinida  
✅ **Contextual**: Respeta el camino de navegación del usuario  
✅ **Robusto**: Tiene valores por defecto si el historial está vacío  
✅ **Reutilizable**: El componente `BackButton` se puede usar en cualquier página  
✅ **Mantenible**: Centraliza la lógica en un servicio y componente dedicados  

## Ejemplo de Uso

```razor
<div class="header-back">
	<BackButton Text="Action_Back" CssClass="my-back-style" DefaultUrl="/home" />
</div>
```

## Notas Técnicas

- El servicio usa `Stack<string>` para mantener un orden LIFO (Last In, First Out)
- Se usa `PathAndQuery` de la URI para evitar problemas con fragmentos y parámetros de query
- El componente mantiene compatibilidad con los estilos CSS existentes
- La localización se maneja a través de `IStringLocalizer<App>`
