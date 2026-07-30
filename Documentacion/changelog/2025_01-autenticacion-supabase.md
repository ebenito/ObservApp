# 2025-01 - Autenticación y Persistencia en Supabase

**Fecha:** Enero 2025  
**Versión:** 10.0.0  
**Alcance:** Implementación completa de autenticación y CRUD de sesiones de observación  
**Estado:** ✅ Implementado y compilable

---

## 📋 Tabla de Contenidos

1. [Objetivo](#objetivo)
2. [Arquitectura](#arquitectura)
3. [Cambios Implementados](#cambios-implementados)
4. [Estructura de Base de Datos](#estructura-de-base-de-datos)
5. [Configuración](#configuración)
6. [Uso en Aplicación](#uso-en-aplicación)
7. [Guía de Instalación](#guía-de-instalación)
8. [API de Servicios](#api-de-servicios)
9. [Localización](#localización)
10. [Archivos Creados/Modificados](#archivos-creados-y-modificados)

---

## 🎯 Objetivo

Implementar un sistema de autenticación con Supabase y persistencia de sesiones de observación astronómica que funcione en:

- **MAUI** (Android + Windows) - Acceso completo a Supabase
- **Blazor WASM** (Web.Client) - Acceso completo a Supabase
- **ASP.NET Core SSR** (Web) - Stubs sin funcionalidad (no-op)

El sistema respeta la arquitectura compartida del proyecto donde todas las interfaces residen en `ObservApp.Shared` y cada host implementa según sus necesidades.

---

## 🏗️ Arquitectura

### Patrón de Diseño

```
┌─────────────────────────────────────────┐
│         ObservApp.Shared                │
│  (Interfaces + ViewModels + Modelos)    │
├─────────────────────────────────────────┤
│ • IAuthService                          │
│ • IObservationService                   │
│ • AuthViewModel (MVVM)                  │
│ • UserProfile / AuthResult              │
│ • ObservationSession                    │
│ • SupabaseService (implementación)      │
└──┬────────────────────┬────────────────┬┘
   │                    │                │
   ▼                    ▼                ▼
┌──────────────┐ ┌─────────────┐ ┌──────────────┐
│   ObservApp  │ │ ObservApp   │ │ ObservApp.Web│
│    (MAUI)    │ │  Web.Client │ │   (SSR)      │
│              │ │  (WASM)     │ │              │
├──────────────┤ ├─────────────┤ ├──────────────┤
│ Supabase Real│ │Supabase Real│ │SsrAuthService│
│  ✓ Autent.   │ │ ✓ Autent.   │ │✗ No-op stubs │
│  ✓ CRUD      │ │ ✓ CRUD      │ │              │
└──────────────┘ └─────────────┘ └──────────────┘
```

### Separación de Responsabilidades

- **Shared**: Contratos (interfaces) + lógica agnóstica
- **MAUI**: Implementación real de Supabase + UI específica
- **Web.Client**: Implementación real de Supabase + UI Blazor
- **Web (SSR)**: Stubs sin estado (Server-Side Rendering no mantiene sesión)

---

## 🔧 Cambios Implementados

### 1. Interfaces de Servicios (ObservApp.Shared/Services/)

#### **IAuthService.cs**
Contrato para autenticación con Supabase:

```csharp
public interface IAuthService
{
	Task<AuthResult> SignInWithEmailAsync(string email, string password);
	Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName);
	Task SignOutAsync();
	Task<UserProfile?> GetCurrentUserAsync();
	Task<bool> IsAuthenticatedAsync();
	event Action<UserProfile?>? OnAuthStateChanged;
}

public record AuthResult(bool Success, string? ErrorMessage, UserProfile? User);
public record UserProfile(string Id, string Email, string? DisplayName);
```

#### **IObservationService.cs**
Contrato para CRUD de sesiones de observación:

```csharp
public interface IObservationService
{
	Task<List<ObservationSession>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<ObservationSession?> GetByIdAsync(Guid id);
	Task<bool> SaveAsync(ObservationSession session);
	Task<bool> DeleteAsync(Guid id);
	string? LastError { get; }
}
```

### 2. Modelo de Datos (ObservApp.Shared/Models/)

#### **ObservationSession.cs**
Modelo mapeado a tabla Supabase con atributos de Postgrest:

```csharp
[Table("observation_sessions")]
public class ObservationSession : BaseModel
{
	[PrimaryKey("id", false)]
	public Guid Id { get; set; }

	[Column("user_id")]
	public string UserId { get; set; }

	[Column("date")]
	public DateTime Date { get; set; }

	[Column("title")]
	public string Title { get; set; }

	// ... más propiedades
}
```

### 3. ViewModel MVVM (ObservApp.Shared/ViewModels/)

#### **AuthViewModel.cs**
ViewModel con propiedades observables usando CommunityToolkit.Mvvm:

```csharp
public partial class AuthViewModel : ObservableObject
{
	[ObservableProperty]
	private string email = string.Empty;

	[ObservableProperty]
	private string password = string.Empty;

	[ObservableProperty]
	private bool isLoading;

	[ObservableProperty]
	private string? errorMessage;

	[RelayCommand]
	public async Task SignInAsync() { ... }

	[RelayCommand]
	public async Task SignUpAsync() { ... }

	[RelayCommand]
	public async Task SignOutAsync() { ... }
}
```

### 4. Implementación de Supabase (ObservApp.Shared/Services/)

#### **SupabaseService.cs**
Implementa ambas interfaces (IAuthService + IObservationService):

- **Autenticación**: `SignIn`, `SignUp`, `SignOut`, `GetCurrentUser`
- **Observaciones**: CRUD completo con filtrado por `user_id`
- **Gestión de errores**: Captura excepciones y expone en `LastError`
- **Eventos**: Dispara `OnAuthStateChanged` en cambios de estado

```csharp
public class SupabaseService : IAuthService, IObservationService
{
	private readonly SupabaseClient _supabase;
	private string? _lastError;

	public async Task InitializeAsync()
	{
		await _supabase.InitializeAsync();
	}

	// IAuthService implementation
	public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
	{
		try
		{
			var session = await _supabase.Auth.SignInWithPassword(email, password);
			var userProfile = new UserProfile(
				session.User.Id,
				session.User.Email ?? email,
				// Leer display_name del metadata de Gotrue
			);
			OnAuthStateChanged?.Invoke(userProfile);
			return new AuthResult(true, null, userProfile);
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return new AuthResult(false, ex.Message, null);
		}
	}

	// IObservationService implementation
	public async Task<List<ObservationSession>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var userId = _supabase.Auth.CurrentUser?.Id;
			var result = await _supabase
				.From<ObservationSession>()
				.Where(o => o.UserId == userId)
				.Get();

			return result?.Models ?? new List<ObservationSession>();
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return new List<ObservationSession>();
		}
	}
}
```

### 5. Stubs para SSR (ObservApp.Web/Services/)

#### **SsrAuthService.cs**
Implementa IAuthService como no-op (sin funcionalidad real):

```csharp
public class SsrAuthService : IAuthService
{
	public Task<AuthResult> SignInWithEmailAsync(string email, string password)
		=> Task.FromResult(new AuthResult(false, "SSR: Autenticación no disponible", null));

	public Task<bool> IsAuthenticatedAsync()
		=> Task.FromResult(false);
}
```

#### **SsrObservationService.cs**
Implementa IObservationService como no-op:

```csharp
public class SsrObservationService : IObservationService
{
	public Task<List<ObservationSession>> GetAllAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult(new List<ObservationSession>());
}
```

### 6. Actualización de AppState

**AppState.cs** ahora sincroniza automáticamente:

```csharp
public UserProfile? CurrentUser
{
	get => _currentUser;
	set
	{
		_currentUser = value;
		IsAuthenticated = value != null;
		UserDisplayName = value?.DisplayName ?? string.Empty;
		OnStateChanged?.Invoke();
	}
}
```

---

## 📊 Estructura de Base de Datos

### Tabla: `observation_sessions`

```sql
CREATE TABLE observation_sessions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  date TIMESTAMPTZ NOT NULL,
  title TEXT NOT NULL,
  notes TEXT,
  latitude DOUBLE PRECISION,
  longitude DOUBLE PRECISION,
  location_name TEXT,
  seeing INTEGER CHECK (seeing >= 1 AND seeing <= 5),
  transparency INTEGER CHECK (transparency >= 1 AND transparency <= 5),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Índices

```sql
CREATE INDEX idx_observation_sessions_user_id ON observation_sessions(user_id);
CREATE INDEX idx_observation_sessions_date ON observation_sessions(date DESC);
```

### Row Level Security (RLS)

Políticas habilitadas (ver `Documentacion/Supabase_SQL_Schema.sql` para script completo):

- **SELECT**: `auth.uid() = user_id` (usuario ve solo sus sesiones)
- **INSERT**: `auth.uid() = user_id` (usuario inserta solo para sí mismo)
- **UPDATE**: `auth.uid() = user_id` (usuario actualiza solo sus sesiones)
- **DELETE**: `auth.uid() = user_id` (usuario elimina solo sus sesiones)

### Trigger Automático

```sql
CREATE TRIGGER update_observation_sessions_updated_at_trigger
BEFORE UPDATE ON observation_sessions
FOR EACH ROW
EXECUTE FUNCTION update_observation_sessions_updated_at();
```

---

## ⚙️ Configuración

### Archivos de Configuración

#### **appsettings.json** (Commiteado - sin claves reales)

```json
{
  "SyncfusionLicenseKey": "",
  "SupabaseUrl": "",
  "SupabaseAnonKey": ""
}
```

#### **appsettings.Local.json** (En .gitignore - Claves reales locales)

```json
{
  "SyncfusionLicenseKey": "...",
  "SupabaseUrl": "https://YOUR-PROJECT.supabase.co",
  "SupabaseAnonKey": "eyJ..."
}
```

### Registro de Servicios

#### En MauiProgram.cs (MAUI)

```csharp
var supabaseUrl = builder.Configuration["SupabaseUrl"] ?? "";
var supabaseKey = builder.Configuration["SupabaseAnonKey"] ?? "";

builder.Services.AddSingleton<SupabaseService>(sp =>
	new SupabaseService(supabaseUrl, supabaseKey));
builder.Services.AddSingleton<IAuthService>(
	sp => sp.GetRequiredService<SupabaseService>());
builder.Services.AddSingleton<IObservationService>(
	sp => sp.GetRequiredService<SupabaseService>());
builder.Services.AddTransient<AuthViewModel>();
```

#### En ObservApp.Web/Program.cs (SSR)

```csharp
builder.Services.AddScoped<IAuthService, SsrAuthService>();
builder.Services.AddScoped<IObservationService, SsrObservationService>();
builder.Services.AddTransient<AuthViewModel>();
```

#### En ObservApp.Web.Client/Program.cs (WASM)

```csharp
var supabaseUrl = builder.Configuration["SupabaseUrl"] ?? "";
var supabaseKey = builder.Configuration["SupabaseAnonKey"] ?? "";

builder.Services.AddSingleton<SupabaseService>(sp =>
	new SupabaseService(supabaseUrl, supabaseKey));
builder.Services.AddSingleton<IAuthService>(
	sp => sp.GetRequiredService<SupabaseService>());
builder.Services.AddSingleton<IObservationService>(
	sp => sp.GetRequiredService<SupabaseService>());
builder.Services.AddTransient<AuthViewModel>();
```

---

## 💻 Uso en Aplicación

### 1. Inicializar Supabase

En `App.xaml.cs` (MAUI) o en bootstrap de Web.Client:

```csharp
protected override async void OnAppearing()
{
	base.OnAppearing();

	var supabaseService = ServiceProvider.GetService<SupabaseService>();
	if (supabaseService != null)
	{
		await supabaseService.InitializeAsync();
	}
}
```

### 2. Inyectar y Usar AuthViewModel

#### En MAUI

```xml
<!-- En tu Page.xaml -->
<StackLayout Padding="20" Spacing="10">
	<Entry Placeholder="Email" Text="{Binding Email}" />
	<Entry Placeholder="Password" Text="{Binding Password}" IsPassword="True" />

	<Button Text="Sign In" Command="{Binding SignInCommand}" />
	<Button Text="Sign Up" Command="{Binding SignUpCommand}" />

	<Label Text="{Binding ErrorMessage}" TextColor="Red" />
	<ActivityIndicator IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}" />
</StackLayout>
```

```csharp
public partial class LoginPage : ContentPage
{
	public LoginPage(AuthViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
```

#### En Blazor

```razor
@page "/login"
@inject AuthViewModel authViewModel

<div class="card">
	<h3>Iniciar sesión</h3>

	<div class="form-group">
		<input type="email" @bind="authViewModel.Email" placeholder="Email" class="form-control" />
	</div>

	<div class="form-group">
		<input type="password" @bind="authViewModel.Password" placeholder="Contraseña" class="form-control" />
	</div>

	<button @onclick="authViewModel.SignInCommand.ExecuteAsync" disabled="@authViewModel.IsLoading">
		@if (authViewModel.IsLoading)
		{
			<span class="spinner"></span> Iniciando sesión...
		}
		else
		{
			<span>Iniciar sesión</span>
		}
	</button>

	@if (!string.IsNullOrEmpty(authViewModel.ErrorMessage))
	{
		<div class="alert alert-danger">@authViewModel.ErrorMessage</div>
	}
</div>
```

### 3. Acceder a Datos de Observación

#### Obtener todas las sesiones

```csharp
var observationService = ServiceProvider.GetService<IObservationService>();
var sessions = await observationService.GetAllAsync();

foreach (var session in sessions)
{
	Console.WriteLine($"{session.Title} - {session.Date}");
}
```

#### Guardar una nueva sesión

```csharp
var newSession = new ObservationSession
{
	Title = "Observación de Plutón",
	Date = DateTime.UtcNow,
	Notes = "Condiciones excelentes",
	Seeing = 4,
	Transparency = 5,
	LocationName = "Observatorio Local"
};

bool success = await observationService.SaveAsync(newSession);
if (!success)
{
	Console.WriteLine($"Error: {observationService.LastError}");
}
```

#### Eliminar una sesión

```csharp
bool deleted = await observationService.DeleteAsync(sessionId);
if (!deleted)
{
	Console.WriteLine($"Error: {observationService.LastError}");
}
```

### 4. Monitorear Cambios de Autenticación

```csharp
var authService = ServiceProvider.GetService<IAuthService>();

authService.OnAuthStateChanged += (userProfile) =>
{
	if (userProfile != null)
	{
		Console.WriteLine($"Autenticado como: {userProfile.DisplayName}");
	}
	else
	{
		Console.WriteLine("Sesión cerrada");
	}
};
```

---

## 📥 Guía de Instalación

### Paso 1: Crear Proyecto Supabase

1. Ve a https://supabase.com
2. Crea un nuevo proyecto
3. Copia las credenciales:
   - **URL de Proyecto**: `https://YOUR-PROJECT.supabase.co`
   - **Clave Anónima**: Disponible en Project Settings > API

### Paso 2: Ejecutar Script SQL

1. En Supabase Dashboard > SQL Editor
2. Crea nueva query
3. Copia contenido de `Documentacion/Supabase_SQL_Schema.sql`
4. Ejecuta

### Paso 3: Configurar Credenciales Locales

Edita `appsettings.Local.json` en MAUI y Web.Client:

```json
{
  "SupabaseUrl": "https://YOUR-PROJECT.supabase.co",
  "SupabaseAnonKey": "eyJ0eXAiOiJKV1QiLCJhbGc..."
}
```

### Paso 4: Compilar y Ejecutar

```bash
dotnet build
dotnet run
```

### Paso 5: Habilitar Autenticación en Supabase

1. Supabase Dashboard > Authentication > Providers
2. Activa "Email"
3. Configura según necesidades (MFA, etc.)

---

## 🔌 API de Servicios

### IAuthService

| Método | Descripción | Retorna |
|--------|-------------|---------|
| `SignInWithEmailAsync(email, password)` | Inicia sesión con email/contraseña | `AuthResult` |
| `SignUpWithEmailAsync(email, password, displayName)` | Registra nuevo usuario | `AuthResult` |
| `SignOutAsync()` | Cierra sesión | `Task` |
| `GetCurrentUserAsync()` | Obtiene perfil del usuario autenticado | `UserProfile?` |
| `IsAuthenticatedAsync()` | Verifica si hay usuario autenticado | `bool` |
| `OnAuthStateChanged` | Evento disparado al cambiar estado | `Action<UserProfile?>?` |

### IObservationService

| Método | Descripción | Retorna |
|--------|-------------|---------|
| `GetAllAsync(cancellationToken)` | Obtiene todas las sesiones del usuario | `List<ObservationSession>` |
| `GetByIdAsync(id)` | Obtiene sesión por ID | `ObservationSession?` |
| `SaveAsync(session)` | Guarda (inserta o actualiza) sesión | `bool` |
| `DeleteAsync(id)` | Elimina sesión | `bool` |
| `LastError` | Último mensaje de error | `string?` |

### AuthViewModel

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Email` | `string` | Email del usuario (observable) |
| `Password` | `string` | Contraseña (observable) |
| `DisplayName` | `string` | Nombre mostrado (observable) |
| `IsLoading` | `bool` | Indicador de carga (observable) |
| `ErrorMessage` | `string?` | Mensaje de error (observable) |
| `SignInCommand` | `IRelayCommand` | Comando para iniciar sesión |
| `SignUpCommand` | `IRelayCommand` | Comando para registrarse |
| `SignOutCommand` | `IRelayCommand` | Comando para cerrar sesión |

---

## 🌍 Localización

Se han agregado **18 claves de localización** a 6 idiomas:

### Idiomas Soportados

- 🇪🇸 Español (es-ES)
- 🇬🇧 English (en-US)
- 🇩🇪 Deutsch (de-DE)
- 🇫🇷 Français (fr-FR)
- 🇮🇹 Italiano (it-IT)
- 🇸🇦 العربية (ar-AR)

### Claves Agregadas

**Autenticación:**
```
Auth_SignIn, Auth_SignUp, Auth_SignOut
Auth_Email, Auth_Password, Auth_DisplayName
Auth_SigningIn, Auth_Error
```

**Sesiones de Observación:**
```
Obs_Sessions, Obs_NewSession, Obs_Title, Obs_Date
Obs_Notes, Obs_Location, Obs_Seeing, Obs_Transparency
Obs_Save, Obs_Delete
```

Ver `Documentacion/Localization_Summary.md` para tabla completa de traducciones.

---

## 📁 Archivos Creados y Modificados

### Archivos Creados

| Ruta | Descripción |
|------|-------------|
| `ObservApp.Shared/Services/IAuthService.cs` | Interfaz de autenticación |
| `ObservApp.Shared/Services/IObservationService.cs` | Interfaz de CRUD |
| `ObservApp.Shared/Services/SupabaseService.cs` | Implementación Supabase |
| `ObservApp.Shared/Models/ObservationSession.cs` | Modelo de sesión de observación |
| `ObservApp.Shared/ViewModels/AuthViewModel.cs` | ViewModel MVVM |
| `ObservApp.Web/Services/SsrAuthService.cs` | Stub para SSR |
| `ObservApp.Web/Services/SsrObservationService.cs` | Stub para SSR |
| `Documentacion/Supabase_SQL_Schema.sql` | Script de BD |
| `Documentacion/Localization_Summary.md` | Resumen de i18n |

### Archivos Modificados

| Ruta | Cambios |
|------|---------|
| `ObservApp.Shared/State/AppState.cs` | Agregada propiedad `CurrentUser` |
| `ObservApp/MauiProgram.cs` | Registrado SupabaseService y AuthViewModel |
| `ObservApp.Web/Program.cs` | Registrado SsrAuthService y AuthViewModel |
| `ObservApp.Web.Client/Program.cs` | Registrado SupabaseService y AuthViewModel |
| `ObservApp.Shared/Resources/Strings/App.es.resx` | Agregadas 18 claves |
| `ObservApp.Shared/Resources/Strings/App.en.resx` | Agregadas 18 claves |
| `ObservApp.Shared/Resources/Strings/App.de.resx` | Agregadas 18 claves |
| `ObservApp.Shared/Resources/Strings/App.fr.resx` | Agregadas 18 claves |
| `ObservApp.Shared/Resources/Strings/App.it.resx` | Agregadas 18 claves |
| `ObservApp.Shared/Resources/Strings/App.ar.resx` | Agregadas 18 claves |
| `ObservApp.Shared/ObservApp.Shared.csproj` | Agregado CommunityToolkit.Mvvm 8.3.0 |

---

## 🔒 Seguridad

### Buenas Prácticas Implementadas

✅ **Nunca hardcodear claves**
- Claves en `appsettings.Local.json` (.gitignore)
- Archivo público: `appsettings.json` vacío

✅ **Row Level Security (RLS)**
- Cada usuario solo ve/modifica sus datos
- Políticas en Supabase (servidor, no confiable solo en cliente)

✅ **Gestión de Sesiones**
- Supabase maneja tokens JWT automáticamente
- Refresh automático en web.Client y MAUI

✅ **Validación**
- Email requerido para autenticación
- Checks en BD: `seeing` y `transparency` entre 1-5
- Eliminación en cascada al eliminar usuario

---

## 🚀 Próximos Pasos (Sugerencias)

1. **UI de Login**
   - Crear página de login bonita en MAUI
   - Crear componente Blazor para login

2. **Gestor de Sesiones**
   - Interfaz para visualizar historial de observaciones
   - Gráficas de seeing/transparency en el tiempo

3. **Sincronización Offline**
   - Guardar localmente cuando no hay conexión
   - Sincronizar cuando hay conexión

4. **Notificaciones**
   - Push notifications para alertas de observación
   - Reminders para sesiones planificadas

5. **Análisis**
   - Dashboard con estadísticas de observación
   - Mapas de ubicaciones más observadas

---

## 🐛 Troubleshooting

### Error: "Usuario no autenticado"
- Verificar credenciales de Supabase en `appsettings.Local.json`
- Comprobar que RLS esté habilitado

### Error: "Table not found"
- Ejecutar script SQL en Supabase Dashboard > SQL Editor
- Verificar nombre de tabla: `observation_sessions`

### Error: "Ambiguous reference Client"
- Verificar imports en SupabaseService.cs
- Debe usar alias: `using SupabaseClient = Supabase.Client;`

### Error: "CORS"
- En Blazor WASM: Verificar que proxy /api/rss-proxy funcione
- En MAUI: No hay restricciones CORS

### AuthViewModel no encontrado
- Verificar import: `using ObservApp.Shared.ViewModels;`
- Verificar registro en Program.cs: `builder.Services.AddTransient<AuthViewModel>();`

---

## 📝 Versionado

- **v1.0.0** - Implementación inicial de autenticación y CRUD
- **v1.5.0** - Soporte multiidioma (6 idiomas)
- **v2.0.0** - Sugerido: UI avanzada y sincronización

---

## 👥 Contribuidores

- Implementación de Supabase: GitHub Copilot (2025)
- Localización: GitHub Copilot (2025)

---

## 📄 Licencia

Este código sigue la licencia del proyecto ObservApp.

---

## 📞 Soporte

Para preguntas o problemas:
1. Revisar `Documentacion/Localization_Summary.md`
2. Revisar `Documentacion/Supabase_SQL_Schema.sql`
3. Consultar documentación oficial: https://supabase.com/docs
4. Revisar issues en: https://github.com/ebenito/ObservApp
