# Vibe Coding — Guía de sesión ObservApp

> Guía práctica para trabajar con GitHub Copilot / Claude Code en este proyecto.  
> Antes de cada sesión: abrir este fichero + `CLAUDE.md` + `Documentacion/Claude/CLAUDE_CONTEXT.md`.

---

## Arranque rápido de sesión

```
1. Abrir Visual Studio 2026 con ObservApp.slnx
2. Rama de trabajo: develop (o rama feature/)
3. Decirle al agente: "Lee CLAUDE.md antes de empezar"
4. Describir la tarea en español
5. Revisar el plan generado antes de confirmar la implementación
```

---

## Cómo pedir cambios al agente

### ✅ Fórmulas que funcionan bien

```
"Añade una nueva calculadora de [X]. Sigue las convenciones de CalculadoraFOV.razor."
"Implementa [funcionalidad] en ObservApp.Shared. Recuerda: solo interfaces inyectadas."
"Añade la clave de localización '[Clave]' a App.es.resx y sus 7 traducciones."
"Crea el servicio I[X]Service en Shared y sus implementaciones Maui, Web y Ssr."
"Añade tests unitarios en ObservApp.Shared.Tests para [clase]."
```

### ❌ Fórmulas que generan problemas

```
"Añade [X] al menú lateral"           → El menú no se toca para nuevas calculadoras
"Usa Preferences directamente en..."   → Solo en MauiSettingsService
"Hardcodea el texto en español"        → Siempre a través de IStringLocalizer
"Instancia un HttpClient nuevo"        → Usar IHttpClientFactory
```

---

## Checklist antes de aceptar código generado

- [ ] ¿El componente solo inyecta interfaces?
- [ ] ¿Todos los textos visibles usan `@L["Clave"]`?
- [ ] ¿La clave existe en `App.es.resx` (y en los otros 7 `.resx`)?
- [ ] ¿El cálculo astronómico está en el service, no en el componente?
- [ ] ¿La altitud se pasa en **km** al constructor `Observer(lat, lon, altKm)`?
- [ ] ¿No hay `<form>` en el Razor?
- [ ] ¿Las páginas privadas tienen `<AuthGuard>`?
- [ ] ¿El build compila sin errores? (`dotnet build ObservApp.slnx`)

---

## Trabajo pendiente (backlog conocido)

### 🔴 Alta prioridad

| # | Tarea | Notas |
|---|---|---|
| 1 | **Calculadora Aumentos y pupila de salida** | Servicio `AumentosCalculatorService` ya existe. Activar tarjeta en `Calculadoras.razor`. |
| 2 | **Persistencia de ubicaciones favoritas en Supabase** | `IFavoriteLocationsService` → nueva implementación `SupabaseFavoriteLocationsService`. |
| 3 | **Tests unitarios para `EfemeridesAstronomyService`** | Cubrir cálculos de altitud solar, fase lunar y eventos. |

### 🟡 Media prioridad

| # | Tarea | Notas |
|---|---|---|
| 4 | **Calculadora Límite de resolución** (Rayleigh + Dawes) | Tarjeta marcada como "próximamente" en `Calculadoras.razor`. |
| 5 | **Calculadora Escala de placa** | arcsec/px y campo total. Para astrofotografía. |
| 6 | **Notificaciones push para eclipses** | Requiere servicio push en MAUI y Web. |
| 7 | **Modo offline completo en WASM** | Service Worker + caché de recursos astronómicos. |

### 🟢 Baja prioridad / mejoras

| # | Tarea | Notas |
|---|---|---|
| 8 | **Exportar sesión de observación a PDF** | Desde `Historial.razor`. |
| 9 | **Compartir sesión** | Generar enlace o imagen para redes sociales. |
| 10 | **Widget de clima nocturno** | Integrar Open-Meteo para ver nubosidad prevista. |
| 11 | **Catálogo DSO con imágenes** | Thumbnails de DSS/ESA Sky en el detalle del objeto. |
| 12 | **Tema claro** | `AppState.OnThemeChanged` ya existe; falta implementar los tokens CSS. |

---

## Patrones de código frecuentes

### Nuevo componente Razor en Shared

```razor
@page "/mi-ruta"
@inject IStringLocalizer<App> L
@inject ISettingsService SettingsService

<div class="mi-componente">
	<h1>@L["MiTitulo"]</h1>
</div>

@code {
	// Estado local del componente
	private bool _showDetail;
}
```

### Nuevo servicio multiplataforma

```
1. ObservApp.Shared/Services/MiDominio/IMiService.cs        → interfaz
2. ObservApp.Shared/Services/MiDominio/MiService.cs         → impl. compartida (si posible)
3. ObservApp/Services/MiDominio/MauiMiService.cs            → impl. MAUI (AddSingleton)
4. ObservApp.Web.Client/Services/MiDominio/WebMiService.cs  → impl. WASM (AddScoped)
5. ObservApp.Web/Services/MiDominio/SsrMiService.cs         → stub SSR (AddScoped)
```

### Añadir clave de localización

```
1. App.es.resx  → clave + valor en español
2. App.en.resx  → traducción inglés
3. App.fr.resx, App.de.resx, App.it.resx, App.pt.resx, App.ru.resx, App.ar.resx → resto
```

### Activar una calculadora existente

En `ObservApp.Shared/Pages/Calculadoras/Calculadoras.razor`:
```razor
<!-- Cambiar calchub-card--soon por calchub-card--available y añadir href -->
<div class="calchub-card calchub-card--available" @onclick='() => Nav.NavigateTo("/calculadoras/mi-ruta")'>
```

---

## Comandos útiles

```powershell
# Build completo
dotnet build ObservApp.slnx

# Tests
dotnet test ObservApp.Shared.Tests/

# Ejecutar web en local
cd ObservApp.Web
dotnet run

# Ver versión calculada por GitVersion
dotnet gitversion /showvariable SemVer
```

---

## Archivos de referencia por tarea

| Tarea | Archivos a leer primero |
|---|---|
| Nueva calculadora | `CalculadoraFOV.razor`, `CalculadoraSolLuna.razor`, `Calculadoras.razor` |
| Nueva página con mapa | `GestionUbicaciones.razor`, `LocationPicker.razor`, `leaflet-map.js` |
| Autenticación | `AuthService.cs`, `AuthViewModel.cs`, `Login.razor`, `MainLayout.razor` |
| Efemérides / datos astronómicos | `EfemeridesAstronomyService.cs`, `EfemeridesUiFormatter.cs`, `EfemeridesViewModel.cs` |
| Noticias / RSS | `RssFeedService.cs`, `ArticleService.cs`, `Senales.razor` |
| Sesiones de observación | `SupabaseService.cs`, `HistorialViewModel.cs`, `Planificacion.razor` |
| Catálogo DSO | `DsoCatalogProvider.cs`, `dso-catalog.json`, `DsoList.razor`, `DsoFilterItem.razor` |
