# Documentación de ObservApp

Índice de documentación técnica y guías del proyecto.

---

## 📚 Documentación Principal

### [DocumentacionTecnica.md](DocumentacionTecnica.md)
Documentación técnica completa del proyecto:
- Estructura de la solución
- Arquitectura general
- Refactorizaciones aplicadas
- Abstracciones de servicios
- Modelos de dominio
- Estado global reactivo
- Patrón MVVM
- Decisiones de diseño
- Trabajo pendiente (TODO)

---

## 🗒️ Changelog

Historial cronológico de cambios y nuevas funcionalidades:

- **[2026_04-arquitectura-base.md](changelog/2026_04-arquitectura-base.md)**  
  Creación de la arquitectura base del proyecto compartido

- **[2026_05-fix-cambio-idioma-sidebar.md](changelog/2026_05-fix-cambio-idioma-sidebar.md)**  
  Corrección del cambio de idioma en el sidebar

- **[2026_05-mejoras-pantalla-inicio-nav-correcciones.md](changelog/2026_05-mejoras-pantalla-inicio-nav-correcciones.md)**  
  Mejoras en la pantalla de inicio y navegación

- **[2026_05-calculadora-sol-luna.md](changelog/2026_05-calculadora-sol-luna.md)**  
  Calculadora Sol y Luna + Gestión de Ubicaciones Favoritas  
  - Nueva calculadora astronómica completa
  - Sistema de ubicaciones favoritas con mapa Leaflet
  - Servicio de geolocalización multiplataforma
  - Búsqueda de lugares (Nominatim)
  - Consulta automática de altitud (Open-Meteo)

---

## 🛠️ Guías de Solución de Problemas

### [guia-solucion-web-no-abre.md](guia-solucion-web-no-abre.md)
Soluciones paso a paso para cuando la versión web no abre el navegador desde Visual Studio:
- Configurar proyecto de inicio
- Proyectos de inicio múltiples
- Verificar launchSettings.json
- Limpiar y reconstruir
- Verificar puertos ocupados
- Reinstalar certificado HTTPS

### [solucion-problemas-ubicaciones.md](solucion-problemas-ubicaciones.md)
Soluciones para problemas con la pantalla de gestión de ubicaciones:
- Mapa no se muestra tras búsqueda (✅ resuelto)
- Leaflet no disponible en MAUI (✅ resuelto)
- Pruebas de verificación

### [arquitectura-web-depuracion.md](arquitectura-web-depuracion.md)
Guía completa sobre la arquitectura web de ObservApp y cómo depurar correctamente:
- Diferencia entre ObservApp.Web y ObservApp.Web.Client
- Arquitectura Blazor WebAssembly con renderizado interactivo
- Cómo depurar servidor vs cliente
- Depuración de componentes Razor
- Flujo de trabajo recomendado
- Problemas comunes y soluciones

---

## 📋 Planes de Implementación

### [implementation_plan.md](implementation_plan.md)
Plan detallado de la implementación del sistema de gestión de ubicaciones favoritas:
- Decisiones de arquitectura (Leaflet vs Syncfusion)
- Consulta automática de altitud (Open-Meteo)
- Abstracción multiplataforma
- Cambios propuestos
- Plan de verificación

---

## 🚀 Scripts de Inicio

En la raíz del proyecto:

- **`start-web.ps1`** — Script PowerShell para iniciar la versión web
- **`start-web.bat`** — Script Batch para iniciar la versión web

Uso:
```powershell
# PowerShell
.\start-web.ps1

# CMD
start-web.bat
```

---

## 📁 Estructura de la Documentación

```
Documentacion/
├── README.md (este archivo)
├── DocumentacionTecnica.md
├── implementation_plan.md
├── arquitectura-web-depuracion.md
├── guia-solucion-web-no-abre.md
├── solucion-problemas-ubicaciones.md
└── changelog/
	├── 2026_04-arquitectura-base.md
	├── 2026_05-fix-cambio-idioma-sidebar.md
	├── 2026_05-mejoras-pantalla-inicio-nav-correcciones.md
	└── 2026_05-calculadora-sol-luna.md
```

---

## 🔗 Enlaces Útiles

- [Repositorio GitHub](https://github.com/ebenito/ObservApp)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Blazor WebAssembly](https://learn.microsoft.com/es-es/aspnet/core/blazor/hosting-models#blazor-webassembly)
- [.NET MAUI](https://learn.microsoft.com/es-es/dotnet/maui/)
- [Syncfusion Blazor Components](https://www.syncfusion.com/blazor-components)
- [Leaflet.js Documentation](https://leafletjs.com/)
- [CosineKitty.AstronomyEngine](https://github.com/cosinekitty/astronomy)

---

## 💡 Consejos para Nuevos Desarrolladores

1. **Empieza leyendo:**
   - `README.md` del proyecto raíz (visión general)
   - `DocumentacionTecnica.md` (arquitectura detallada)
   - `arquitectura-web-depuracion.md` (cómo depurar correctamente)

2. **Para ejecutar el proyecto:**
   - Versión web: `.\start-web.bat` o establecer `ObservApp.Web` como inicio en VS
   - Versión MAUI: Establecer `ObservApp` como proyecto de inicio en VS

3. **Para entender cambios recientes:**
   - Revisa el changelog más reciente en `changelog/`

4. **Si encuentras un problema:**
   - Consulta primero las guías de solución de problemas
   - Revisa los issues cerrados en GitHub
   - Verifica el changelog por si ya fue reportado/solucionado

---

## 📝 Convenciones de Documentación

- **Changelog:** Nombrado como `YYYY_MM-descripcion-breve.md`
- **Guías de solución:** Prefijo `guia-` o `solucion-`
- **Formato:** Markdown con tablas, listas y bloques de código
- **Idioma:** Español (documentación interna), código y comentarios en inglés cuando sea estándar

---

**Última actualización:** Mayo 2026
