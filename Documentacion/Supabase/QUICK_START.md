# 🚀 Guía Rápida - Autenticación en Supabase

**Para:** Desarrolladores que quieren empezar rápido  
**Tiempo:** 5-10 minutos  
**Requisitos:** Proyecto compilable + Credenciales Supabase

---

## ⚡ TL;DR - Las 5 Cosas Que Necesitas Saber

1. **Interfaz IAuthService**: Maneja login/signup/logout
2. **Interfaz IObservationService**: CRUD de sesiones de observación
3. **AuthViewModel**: Propiedades observables para UI
4. **SupabaseService**: Implementación real (MAUI + Web.Client)
5. **SsrAuthService/SsrObservationService**: Stubs para SSR

---

## 🔧 Configuración (5 minutos)

### 1. Obtén credenciales Supabase

```
https://supabase.com → New Project → Settings > API
Copia:
- Project URL: https://YOUR-PROJECT.supabase.co
- Anon Key: eyJ0eXAi...
```

### 2. Edita appsettings.Local.json

**En:** `ObservApp/appsettings.Local.json`  
**Y:** `ObservApp.Web.Client/wwwroot/appsettings.Local.json`

```json
{
  "SupabaseUrl": "https://YOUR-PROJECT.supabase.co",
  "SupabaseAnonKey": "eyJ0eXAi..."
}
```

### 3. Ejecuta SQL

**En:** Supabase Dashboard > SQL Editor

```sql
-- Copiar contenido de: Documentacion/Supabase_SQL_Schema.sql
-- Pegar y ejecutar
```

### 4. Habilita Email Auth

**En:** Supabase Dashboard > Authentication > Providers  
→ Activa "Email"

---

## 💻 Uso Básico

### En MAUI

```xml
<!-- LoginPage.xaml -->
<StackLayout Padding="20" Spacing="10">
	<Label Text="@AppStrings.Auth_Email" />
	<Entry Text="{Binding Email}" />

	<Label Text="@AppStrings.Auth_Password" />
	<Entry Text="{Binding Password}" IsPassword="True" />

	<Button Text="@AppStrings.Auth_SignIn" 
			Command="{Binding SignInCommand}" />

	<Label Text="{Binding ErrorMessage}" TextColor="Red" />
</StackLayout>
```

```csharp
// LoginPage.xaml.cs
public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
		BindingContext = new AuthViewModel(
			ServiceProvider.GetService<IAuthService>(),
			ServiceProvider.GetService<AppState>()
		);
	}
}
```

### En Blazor

```razor
<!-- Login.razor -->
@page "/login"
@inject AuthViewModel authViewModel

<div class="container">
	<h3>Iniciar sesión</h3>

	<input type="email" @bind="authViewModel.Email" 
		   placeholder="Email" class="form-control" />

	<input type="password" @bind="authViewModel.Password" 
		   placeholder="Contraseña" class="form-control" />

	<button @onclick="authViewModel.SignInCommand.ExecuteAsync"
			disabled="@authViewModel.IsLoading">
		@(authViewModel.IsLoading ? "Iniciando..." : "Iniciar sesión")
	</button>

	@if (!string.IsNullOrEmpty(authViewModel.ErrorMessage))
	{
		<div class="alert alert-danger">@authViewModel.ErrorMessage</div>
	}
</div>

@code {
	protected override void OnInitialized()
	{
		// authViewModel ya está inyectado
	}
}
```

---

## 📋 Operaciones Comunes

### Login

```csharp
var authService = ServiceProvider.GetService<IAuthService>();
var result = await authService.SignInWithEmailAsync("user@example.com", "password");

if (result.Success)
{
	// Ir a página principal
	Shell.Current.GoToAsync("main");
}
else
{
	// Mostrar error: result.ErrorMessage
}
```

### Registrarse

```csharp
var result = await authService.SignUpWithEmailAsync(
	"newuser@example.com", 
	"password", 
	"John Doe"
);
```

### Verificar si está autenticado

```csharp
bool isAuth = await authService.IsAuthenticatedAsync();
if (!isAuth) Shell.Current.GoToAsync("login");
```

### Obtener usuario actual

```csharp
var user = await authService.GetCurrentUserAsync();
if (user != null)
{
	Console.WriteLine($"Hola {user.DisplayName}");
}
```

### Logout

```csharp
await authService.SignOutAsync();
Shell.Current.GoToAsync("login");
```

---

## 📊 Trabajar con Sesiones

### Obtener todas tus sesiones

```csharp
var obsService = ServiceProvider.GetService<IObservationService>();
var sessions = await obsService.GetAllAsync();

foreach (var session in sessions)
{
	Console.WriteLine($"{session.Title} - {session.Date}");
}
```

### Crear nueva sesión

```csharp
var session = new ObservationSession
{
	Title = "Observación de Saturno",
	Date = DateTime.UtcNow,
	Latitude = 40.4168,
	Longitude = -3.7038,
	LocationName = "Madrid",
	Notes = "Anillos bien visibles",
	Seeing = 4,
	Transparency = 4
};

bool saved = await obsService.SaveAsync(session);
if (!saved)
{
	Console.WriteLine($"Error: {obsService.LastError}");
}
```

### Obtener una sesión

```csharp
var session = await obsService.GetByIdAsync(sessionId);
if (session != null)
{
	Console.WriteLine(session.Title);
}
```

### Actualizar sesión

```csharp
// Modificar sesión existente
session.Notes = "Nueva observación";
await obsService.SaveAsync(session);
```

### Eliminar sesión

```csharp
bool deleted = await obsService.DeleteAsync(sessionId);
if (!deleted)
{
	Console.WriteLine($"Error: {obsService.LastError}");
}
```

---

## 🌍 Usar Localización

### En XAML (MAUI)

```xml
<Label Text="{Static local:AppStrings.Auth_SignIn}" />
```

### En C# (MAUI/Blazor)

```csharp
using ObservApp.Shared.Resources.Strings;

string text = AppStrings.Auth_Email;
```

### En Razor (Blazor)

```razor
@using ObservApp.Shared.Resources.Strings

<h3>@AppStrings.Obs_Sessions</h3>
```

### Idiomas disponibles

- 🇪🇸 Español: automático si `CultureInfo.CurrentCulture = es-*`
- 🇬🇧 Inglés: automático si `CultureInfo.CurrentCulture = en-*`
- 🇩🇪 Alemán, 🇫🇷 Francés, 🇮🇹 Italiano, 🇸🇦 Árabe: similar

---

## 🐛 Errores Comunes

### ❌ "Usuario no autenticado"
```
Solución: Verificar credenciales en appsettings.Local.json
```

### ❌ "Table not found"
```
Solución: Ejecutar Documentacion/Supabase_SQL_Schema.sql en Supabase
```

### ❌ "AuthViewModel not found"
```
Solución: Agregar using ObservApp.Shared.ViewModels;
```

### ❌ "IAuthService is null"
```
Solución: Verificar que esté registrado en Program.cs:
builder.Services.AddSingleton<IAuthService>(...)
```

### ❌ "Email already exists"
```
Solución: Usar otro email o eliminar usuario en Supabase Dashboard
```

---

## 📚 Documentación Completa

Para más detalles, ver: `Documentacion/changelog/2025_01-autenticacion-supabase.md`

---

## ✅ Checklist para Producción

- [ ] Credenciales en appsettings.Local.json (no commiteado)
- [ ] Script SQL ejecutado en Supabase
- [ ] Email auth habilitado en Supabase
- [ ] Compilación exitosa (`dotnet build`)
- [ ] Prueba login exitosa
- [ ] Prueba signup exitosa
- [ ] Prueba logout exitosa
- [ ] Prueba crear sesión
- [ ] Prueba eliminar sesión
- [ ] RLS habilitado en Supabase (verificar políticas)
- [ ] Error handling en UI

---

## 🆘 ¿Necesitas ayuda?

1. **Preguntas sobre Supabase**: https://supabase.com/docs
2. **Preguntas sobre MAUI**: https://learn.microsoft.com/en-us/dotnet/maui/
3. **Preguntas sobre Blazor**: https://learn.microsoft.com/en-us/aspnet/core/blazor/
4. **Issues del proyecto**: https://github.com/ebenito/ObservApp/issues

---

**Happy coding! 🚀**
