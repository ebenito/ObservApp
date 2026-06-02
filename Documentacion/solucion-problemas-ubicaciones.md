# Solución de Problemas - Gestión de Ubicaciones

## ✅ Problemas Resueltos

### 1. Mapa no se muestra después de búsqueda

**Causa:** Cuando el usuario realizaba una búsqueda antes de que el mapa se renderizara completamente, `SincronizarMapaPosicionAsync()` intentaba actualizar un mapa que aún no existía.

**Solución aplicada:**
- Modificada la función `SincronizarMapaPosicionAsync()` para verificar si el mapa existe antes de actualizar.
- Si el mapa no existe, se inicializa automáticamente.

```csharp
// Verificar si el mapa ya existe
var existe = await JS.InvokeAsync<bool>("eval", 
	$"!!document.getElementById('leaflet-fav-map')?._leaflet_map");

if (existe)
{
	// Actualizar posición
	await JS.InvokeVoidAsync("window.observApp.updateMapPosition", ...);
}
else
{
	// Inicializar mapa nuevo
	await InicializarMapaAsync();
}
```

### 2. Leaflet no disponible en la versión MAUI

**Causa:** El archivo `ObservApp\wwwroot\index.html` no incluía las referencias a Leaflet CSS/JS.

**Solución aplicada:**
- Añadido CSS de Leaflet en el `<head>`:
  ```html
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" crossorigin="" />
  ```
- Añadido JS de Leaflet y el helper antes del cierre de `</body>`:
  ```html
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" crossorigin=""></script>
  <script src="_content/ObservApp.Shared/leaflet-map.js"></script>
  ```

---

## ⚠️ Problema pendiente: Versión Web no abre navegador

### Diagnóstico

El proyecto web **compila correctamente** sin errores:
```
✓ ObservApp.Shared.dll
✓ ObservApp.Web.Client.dll (Blazor output)
✓ ObservApp.Web.dll
```

Todos los servicios SSR están correctamente implementados y registrados:
- ✅ `SsrFavoriteLocationsService`
- ✅ `SsrGeolocationService`
- ✅ `SsrLocalizationService`
- ✅ `SsrSettingsService`

### Posibles causas

1. **Proyecto de inicio incorrecto en Visual Studio**
   - Visual Studio puede estar configurado para iniciar `ObservApp` (MAUI) en lugar de `ObservApp.Web`.

2. **Perfil de lanzamiento incorrecto**
   - El proyecto web tiene tres perfiles disponibles: `http`, `https`, `IIS Express`.
   - Puede que el perfil seleccionado no tenga `launchBrowser: true`.

3. **Puerto ya en uso**
   - Puertos configurados: `http://localhost:5249` y `https://localhost:7294`.
   - Otro proceso podría estar usando estos puertos.

### Soluciones propuestas

#### Opción 1: Configurar proyecto de inicio en Visual Studio

1. En el **Explorador de soluciones**, clic derecho en `ObservApp.Web`.
2. Seleccionar **"Establecer como proyecto de inicio"**.
3. Verificar que en la barra de herramientas aparezca `ObservApp.Web` como proyecto activo.
4. Ejecutar (F5).

#### Opción 2: Ejecutar desde línea de comandos

```powershell
# Desde la raíz del workspace
cd ObservApp.Web
dotnet run --launch-profile https
```

Esto debería:
- Compilar el proyecto web.
- Iniciar el servidor en `https://localhost:7294`.
- **Abrir automáticamente el navegador** si `launchBrowser: true` está configurado.

#### Opción 3: Verificar perfiles de lanzamiento

Editar `ObservApp.Web\Properties\launchSettings.json` y asegurarse de que el perfil activo tenga:

```json
{
  "commandName": "Project",
  "launchBrowser": true,  // ← Debe estar en true
  "environmentVariables": {
	"ASPNETCORE_ENVIRONMENT": "Development"
  },
  "applicationUrl": "https://localhost:7294;http://localhost:5249"
}
```

#### Opción 4: Verificar que no haya conflictos de puerto

```powershell
# Verificar si los puertos están en uso
netstat -ano | findstr :5249
netstat -ano | findstr :7294

# Si están ocupados, cambiar los puertos en launchSettings.json
```

---

## 🧪 Pruebas de verificación

### Verificar mapa en MAUI
1. Ejecutar la app MAUI.
2. Ir a **Configuración → Gestionar ubicaciones favoritas**.
3. Buscar "Madrid" en el buscador Nominatim.
4. **Verificar:** El mapa debe aparecer centrado en Madrid con el marcador.
5. Arrastrar el marcador.
6. **Verificar:** Las coordenadas lat/lon se actualizan en tiempo real.

### Verificar mapa en Web
1. Ejecutar `ObservApp.Web` (ver opciones arriba).
2. Abrir navegador en `https://localhost:7294`.
3. Ir a **Configuración → Gestionar ubicaciones favoritas**.
4. Buscar "Barcelona" en el buscador Nominatim.
5. **Verificar:** El mapa debe aparecer centrado en Barcelona.
6. Arrastrar el marcador en el navegador.
7. **Verificar:** Las coordenadas se actualizan.

### Verificar consulta automática de altitud
1. En la pantalla de gestión de ubicaciones, buscar "Teide, Tenerife".
2. Seleccionar el resultado.
3. **Verificar:** El spinner "Consultando altitud..." aparece brevemente.
4. **Verificar:** El campo de altitud se rellena automáticamente con ~3718 metros.

---

## 📝 Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `ObservApp\wwwroot\index.html` | ✏️ Añadidos CSS/JS de Leaflet + helper `leaflet-map.js` |
| `ObservApp.Shared\Pages\GestionUbicaciones.razor` | ✏️ Mejorado `SincronizarMapaPosicionAsync` para inicializar mapa si no existe |

---

## ✨ Próximos pasos recomendados

1. **Documentar en el README** cómo ejecutar tanto la versión MAUI como Web.
2. **Añadir manejo de errores visual** en la UI cuando Leaflet no carga (por ejemplo, sin conexión a internet para los CDN).
3. **Considerar bundle local de Leaflet** para la versión MAUI (evitar dependencia de CDN en dispositivos sin conexión).
4. **Añadir tests** para los servicios de ubicaciones favoritas (MAUI, Web, SSR).
