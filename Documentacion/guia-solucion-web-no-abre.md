# Guía de Solución: Versión Web no se Abre

## Problema
La versión web de ObservApp no abre el navegador al ejecutarse desde Visual Studio 2026, aunque el proyecto compila correctamente.

## ⚠️ Importante: ObservApp.Web vs ObservApp.Web.Client

**ObservApp** utiliza la arquitectura **Blazor WebAssembly con renderizado interactivo**:

- **`ObservApp.Web`** — Servidor ASP.NET Core que sirve la aplicación
- **`ObservApp.Web.Client`** — Cliente WebAssembly que se ejecuta en el navegador

### ¿Qué proyecto debo ejecutar?

✅ **`ObservApp.Web`** — Este es el proyecto que debes establecer como inicio  
❌ **`ObservApp.Web.Client`** — No puede ejecutarse solo, necesita el servidor

**El cliente WASM se descarga y ejecuta automáticamente en el navegador cuando accedes al servidor.**

---

## Diagnóstico Realizado

✅ **Proyecto Web existe:** `ObservApp.Web\ObservApp.Web.csproj`  
✅ **Compila sin errores:** Verificado  
✅ **Configuración de puertos:** 
- HTTP: `http://localhost:5249`
- HTTPS: `https://localhost:7294`

❌ **Servidor no se ejecuta:** No hay proceso escuchando en los puertos configurados

## Solución Paso a Paso

### Opción 1: Configurar Proyecto de Inicio en Visual Studio 2026

1. **Abrir el Explorador de Soluciones**
   - Ir a `Ver` → `Explorador de soluciones` (o `Ctrl+Alt+L`)

2. **Establecer ObservApp.Web como proyecto de inicio**
   - Clic derecho en el proyecto `ObservApp.Web`
   - Seleccionar **"Establecer como proyecto de inicio"**
   - El proyecto debe aparecer en **negrita** en el Explorador de Soluciones

3. **Verificar el perfil de depuración**
   - En la barra de herramientas superior, junto al botón de ejecución (▶), debe aparecer un desplegable
   - Asegurarse de que está seleccionado `https` o `http` (no `IIS Express` a menos que lo prefieras)
   - Debe verse algo como: `▶ https | ObservApp.Web`

4. **Ejecutar**
   - Presionar `F5` o hacer clic en el botón verde de ejecución
   - El navegador debería abrirse automáticamente en `https://localhost:7294`

---

### Opción 2: Usar Proyectos de Inicio Múltiples (Recomendado para desarrollo)

Si quieres ejecutar tanto MAUI como Web simultáneamente:

1. **Clic derecho en la solución** (nodo raíz en el Explorador de Soluciones)
2. **Propiedades**
3. **Proyectos de inicio**
4. Seleccionar **"Proyectos de inicio múltiples"**
5. Configurar:
   - `ObservApp` → Acción: **Iniciar**
   - `ObservApp.Web` → Acción: **Iniciar**
   - Los demás → Acción: **Ninguno**
6. **Aplicar** → **Aceptar**
7. Presionar `F5`

Ahora ambas aplicaciones se ejecutarán simultáneamente.

---

### Opción 3: Ejecutar desde Terminal Integrado de Visual Studio

1. **Abrir Terminal de Visual Studio**
   - `Ver` → `Terminal` (o `Ctrl+ñ`)

2. **Navegar al proyecto web**
   ```powershell
   cd ObservApp.Web
   ```

3. **Ejecutar el servidor**
   ```powershell
   dotnet run --launch-profile https
   ```

4. **Resultado esperado:**
   ```
   info: Microsoft.Hosting.Lifetime[14]
		 Now listening on: https://localhost:7294
   info: Microsoft.Hosting.Lifetime[14]
		 Now listening on: http://localhost:5249
   info: Microsoft.Hosting.Lifetime[0]
		 Application started. Press Ctrl+C to shut down.
   ```

5. **Abrir navegador manualmente:**
   - Si no se abre automáticamente, ir a: `https://localhost:7294`

---

### Opción 4: Verificar y Corregir launchSettings.json

Si ninguna de las opciones anteriores funciona, verifica la configuración de lanzamiento:

1. **Abrir el archivo:**
   `ObservApp.Web\Properties\launchSettings.json`

2. **Asegurarse de que el perfil `https` tiene `launchBrowser: true`:**
   ```json
   {
	 "profiles": {
	   "https": {
		 "commandName": "Project",
		 "launchBrowser": true,  // ← Debe estar en true
		 "environmentVariables": {
		   "ASPNETCORE_ENVIRONMENT": "Development"
		 },
		 "dotnetRunMessages": true,
		 "applicationUrl": "https://localhost:7294;http://localhost:5249"
	   }
	 }
   }
   ```

3. **Si `launchBrowser` está en `false`, cambiarlo a `true`**

4. **Guardar el archivo** y volver a ejecutar desde Visual Studio

---

### Opción 5: Limpiar y Reconstruir

A veces los archivos de compilación intermedios pueden causar problemas:

1. **En Visual Studio:**
   - `Compilar` → `Limpiar solución`
   - Esperar a que termine
   - `Compilar` → `Recompilar solución`

2. **O desde terminal:**
   ```powershell
   dotnet clean
   dotnet build
   cd ObservApp.Web
   dotnet run --launch-profile https
   ```

---

## Verificación de Puerto Ocupado

Si el servidor arranca pero el navegador no se abre, puede ser que los puertos estén ocupados:

### Windows:
```powershell
# Verificar si el puerto 7294 está en uso
netstat -ano | findstr :7294

# Si aparece algo, el puerto está ocupado. Obtener el PID y detener el proceso:
taskkill /PID <número_de_PID> /F

# O cambiar el puerto en launchSettings.json
```

---

## Solución Rápida (Atajo)

Si tienes prisa y solo quieres ejecutar la versión web:

1. Abrir PowerShell en la raíz del proyecto
2. Ejecutar:
   ```powershell
   cd ObservApp.Web
   dotnet watch run --launch-profile https
   ```
3. El navegador debería abrirse automáticamente
4. Además, `watch` recargará automáticamente al hacer cambios en el código

---

## Configuración Recomendada para Desarrollo

Para trabajar eficientemente con ambas versiones (MAUI + Web):

1. **Proyecto de inicio:** `ObservApp.Web` (o proyectos múltiples)
2. **Perfil de depuración:** `https`
3. **Hot Reload activado:** `Herramientas` → `Opciones` → `Depuración` → Activar Hot Reload
4. **Terminal integrado:** Mantener abierto para ver logs del servidor

---

## Si Nada Funciona: Reinstalar Certificado de Desarrollo HTTPS

En raras ocasiones, el certificado de desarrollo local puede estar corrupto:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Confirmar cuando pregunte si confías en el certificado, luego reiniciar Visual Studio.

---

## Comprobación Final

Después de aplicar cualquiera de las soluciones anteriores, verifica:

1. ✅ El servidor arranca sin errores
2. ✅ Se muestra: `Now listening on: https://localhost:7294`
3. ✅ El navegador se abre automáticamente (o abrirlo manualmente)
4. ✅ La aplicación carga correctamente (pantalla de inicio de ObservApp)
5. ✅ Puedes navegar a `/configuracion/ubicaciones` y ver el mapa Leaflet

---

## Próximos Pasos

Una vez que la versión web esté funcionando:

1. Probar la funcionalidad de ubicaciones favoritas
2. Verificar que el mapa Leaflet carga correctamente
3. Probar búsqueda de lugares con Nominatim
4. Verificar consulta automática de altitud
5. Guardar una ubicación y recargar la página para verificar persistencia en localStorage

---

## Contacto / Soporte

Si después de todos estos pasos la versión web sigue sin abrirse:

1. Verificar los logs de Visual Studio en la ventana de Salida
2. Revisar si hay excepciones en la consola del navegador (F12)
3. Comprobar que no hay firewall bloqueando localhost
4. Verificar que .NET 10 SDK está correctamente instalado: `dotnet --version`
