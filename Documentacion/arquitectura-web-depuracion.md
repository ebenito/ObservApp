# Arquitectura Web de ObservApp y Depuración

## Arquitectura Blazor WebAssembly con Renderizado Interactivo

ObservApp Web utiliza el nuevo modelo de **Blazor United** (.NET 10) que combina:

```
┌─────────────────────────────────────────────────┐
│         Navegador (Cliente)                     │
│                                                 │
│  1. Carga HTML/CSS/JS inicial                  │
│  2. Descarga ObservApp.Web.Client.dll           │
│  3. Ejecuta Blazor WASM runtime                │
│  4. Componentes interactivos en el navegador   │
└────────────┬────────────────────────────────────┘
			 │ HTTPS
			 │ WebSocket (SignalR para depuración)
┌────────────▼────────────────────────────────────┐
│      ObservApp.Web (Servidor ASP.NET Core)      │
│                                                 │
│  - Sirve archivos estáticos                    │
│  - Renderizado inicial SSR (Server-Side)       │
│  - Host para Blazor WebAssembly                │
│  - Proxy de depuración                         │
└─────────────────────────────────────────────────┘
```

## Proyectos Web

| Proyecto | Tipo | Propósito |
|----------|------|-----------|
| **ObservApp.Web** | ASP.NET Core Host | Servidor que sirve la aplicación y actúa como host SSR |
| **ObservApp.Web.Client** | Blazor WebAssembly | Código que se ejecuta en el navegador del usuario |
| **ObservApp.Shared** | Razor Class Library | Componentes y lógica compartida entre todos los proyectos |

## ¿Por qué no puedo ejecutar ObservApp.Web.Client directamente?

`ObservApp.Web.Client` es un proyecto de tipo `Sdk="Microsoft.NET.Sdk.BlazorWebAssembly"`, que produce archivos `.dll` que se ejecutan en el navegador usando **WebAssembly**.

**No tiene servidor web propio**, por eso necesita:
1. Un servidor ASP.NET Core que sirva el archivo `index.html`
2. Servir los archivos `_framework/*.dll` del cliente
3. Proporcionar el proxy de depuración WebSocket

Todo esto lo proporciona `ObservApp.Web`.

---

## Cómo Depurar Correctamente

### Opción 1: Depurar solo el servidor (más común)

1. **Establecer `ObservApp.Web` como proyecto de inicio**
   - Clic derecho en `ObservApp.Web` → "Establecer como proyecto de inicio"

2. **Seleccionar perfil `https`** en la barra de herramientas

3. **Presionar F5**
   - El servidor arrancará en `https://localhost:7294`
   - El navegador se abrirá automáticamente
   - El cliente WASM se descargará y ejecutará

4. **Colocar breakpoints:**
   - ✅ En archivos `.razor` de `ObservApp.Shared` (se depuran en el navegador)
   - ✅ En `Program.cs` de `ObservApp.Web` (se depuran en el servidor)
   - ✅ En servicios SSR de `ObservApp.Web/Services` (servidor)

---

### Opción 2: Depurar código del navegador (WASM)

Cuando depuras código Blazor que se ejecuta **en el navegador** (componentes Razor, servicios WASM):

1. **Ejecutar `ObservApp.Web` normalmente** (F5)

2. **Abrir las herramientas de desarrollo del navegador:**
   - Presionar **Shift+Alt+D** en el navegador
   - O en Chrome: `chrome://inspect/#devices` → "inspect" en la instancia de Blazor

3. **Usar los DevTools del navegador:**
   - Los breakpoints de Visual Studio **no funcionan** en código WASM
   - Debes usar la consola de depuración del navegador
   - Puedes ver variables, call stack, etc.

**Alternativa más sencilla:** Usar `Console.WriteLine()` en el código Razor y verlo en la consola del navegador (F12 → Console).

---

### Opción 3: Depurar servidor + cliente simultáneamente

Si necesitas depurar tanto código del servidor como del cliente:

1. **Iniciar con depuración:** F5 en `ObservApp.Web`

2. **Adjuntar al proceso del navegador:**
   - `Depurar` → `Asociar al proceso`
   - Buscar el proceso del navegador (ej. `msedge.exe`, `chrome.exe`)
   - Seleccionar el tipo de código: `Managed (CoreCLR)` + `Script`

3. **Ahora puedes:**
   - Breakpoints en `ObservApp.Web` (servidor) → Visual Studio
   - Breakpoints en `ObservApp.Shared` renderizado en cliente → DevTools del navegador

---

## Configuración de Perfiles de Depuración

### ObservApp.Web (Servidor)

Archivo: `ObservApp.Web\Properties\launchSettings.json`

```json
{
  "profiles": {
	"https": {
	  "commandName": "Project",
	  "launchBrowser": true,
	  "applicationUrl": "https://localhost:7294;http://localhost:5249"
	}
  }
}
```

### ObservApp.Web.Client (Cliente WASM)

Archivo: `ObservApp.Web.Client\Properties\launchSettings.json`

```json
{
  "profiles": {
	"ObservApp.Web.Client": {
	  "commandName": "Project",
	  "launchBrowser": true,
	  "applicationUrl": "https://localhost:7294;http://localhost:5249"
	}
  }
}
```

**Nota:** Ambos apuntan a la misma URL porque el cliente WASM necesita el servidor corriendo.

---

## Problemas Comunes y Soluciones

### ❌ "No se puede iniciar ObservApp.Web.Client"

**Causa:** Estás intentando ejecutar el proyecto cliente sin el servidor.

**Solución:** Ejecuta `ObservApp.Web` en su lugar.

---

### ❌ "El navegador se abre pero muestra 'No se puede acceder a este sitio'"

**Causa:** El servidor no está corriendo o está en un puerto diferente.

**Solución:**
1. Verifica que `ObservApp.Web` está corriendo (debe aparecer en la ventana de salida)
2. Verifica la URL: debe ser `https://localhost:7294`
3. Si falla, ejecuta desde terminal: `.\start-web.bat`

---

### ❌ "Los breakpoints en archivos .razor no funcionan"

**Causa:** Dependiendo de dónde se renderiza el componente (servidor o cliente), los breakpoints se gestionan de forma diferente.

**Solución:**

| Componente renderizado en | Cómo depurar |
|---|---|
| **Servidor (SSR)** | Breakpoints en Visual Studio ✅ |
| **Cliente (WASM)** | DevTools del navegador (Shift+Alt+D) |

**Tip:** ObservApp usa `@rendermode="InteractiveWebAssembly"`, así que la mayoría de componentes se ejecutan en el **cliente** (navegador).

---

### ❌ "Cambios en archivos .razor no se reflejan al guardar"

**Causa:** Hot Reload puede fallar en ciertos escenarios.

**Solución:**
1. **Opción 1:** Usar `dotnet watch` en lugar de `dotnet run`:
   ```powershell
   cd ObservApp.Web
   dotnet watch run --launch-profile https
   ```

2. **Opción 2:** Recargar manualmente el navegador (F5 en el navegador, no en VS)

3. **Opción 3:** Detener (Shift+F5) y reiniciar (F5) la depuración en Visual Studio

---

## Flujo de Trabajo Recomendado

### Para desarrollo rápido sin depuración:

```powershell
# En la raíz del proyecto
.\start-web.bat

# O con watch para hot reload automático
cd ObservApp.Web
dotnet watch run --launch-profile https
```

### Para depuración de servidor:

1. Establecer `ObservApp.Web` como proyecto de inicio
2. F5 en Visual Studio
3. Colocar breakpoints en servicios SSR, `Program.cs`, etc.

### Para depuración de componentes Razor:

1. F5 en `ObservApp.Web` desde Visual Studio
2. Shift+Alt+D en el navegador
3. Abrir DevTools → Sources → Colocar breakpoints en archivos `.razor`

### Para desarrollo de UI/estilos:

1. Ejecutar `.\start-web.bat` o `dotnet watch run`
2. Editar archivos `.razor`, `.css`, `.js`
3. Los cambios se reflejan automáticamente sin reiniciar

---

## Resumen de Comandos Útiles

| Acción | Comando |
|--------|---------|
| Ejecutar servidor con hot reload | `dotnet watch run --project ObservApp.Web --launch-profile https` |
| Compilar todo | `dotnet build` |
| Limpiar artefactos | `dotnet clean` |
| Publicar para producción | `dotnet publish ObservApp.Web -c Release` |
| Ver logs detallados | `dotnet run --verbosity detailed --project ObservApp.Web` |

---

## Estructura de Renderizado

```
App.razor (@rendermode="InteractiveWebAssembly")
│
├── Routes.razor
│   └── Páginas de ObservApp.Shared/*.razor
│       ├── Home.razor
│       ├── Calculadoras.razor
│       ├── CalculadoraSolLuna.razor
│       └── GestionUbicaciones.razor
│
└── Todos estos componentes se renderizan en el CLIENTE (navegador)
	usando ObservApp.Web.Client.dll descargado via WebAssembly
```

**Por eso `ObservApp.Web.Client` contiene los servicios WASM:**
- `WebGeolocationService` (usa `navigator.geolocation` via JSInterop)
- `WebFavoriteLocationsService` (usa `localStorage` via JSInterop)
- `WebLocalizationService` (usa `localStorage` via JSInterop)

Y `ObservApp.Web` contiene los servicios SSR stub:
- `SsrGeolocationService` (devuelve null)
- `SsrFavoriteLocationsService` (devuelve lista vacía)

---

## Referencias

- [Blazor WebAssembly](https://learn.microsoft.com/es-es/aspnet/core/blazor/hosting-models#blazor-webassembly)
- [Depuración de Blazor WebAssembly](https://learn.microsoft.com/es-es/aspnet/core/blazor/debug)
- [Blazor United (.NET 8+)](https://learn.microsoft.com/es-es/aspnet/core/blazor/components/render-modes)
