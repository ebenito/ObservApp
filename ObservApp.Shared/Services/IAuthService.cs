namespace ObservApp.Shared.Services;

using Supabase.Gotrue;

/// <summary>
/// Interfaz para autenticación directa contra Supabase Auth.
/// </summary>
public interface IAuthService
{
	/// <summary>
	/// Último error de autenticación normalizado para UI.
	/// </summary>
	string? LastError { get; }

	/// <summary>
	/// Usuario autenticado en la sesión actual.
	/// </summary>
	User? CurrentUser { get; }

	/// <summary>
	/// Indica si existe sesión autenticada activa.
	/// </summary>
	bool IsAuthenticated { get; }

	/// <summary>
	/// Registra un usuario con email y contraseña.
	/// </summary>
	Task<bool> SignUpAsync(string email, string password);

	/// <summary>
	/// Inicia sesión con email y contraseña.
	/// </summary>
	Task<bool> LoginAsync(string email, string password);

	/// <summary>
	/// Cierra sesión y limpia almacenamiento local.
	/// </summary>
	Task LogoutAsync();

	/// <summary>
	/// Intenta restaurar la sesión desde almacenamiento local.
	/// </summary>
	Task<bool> TryRestoreSessionAsync();

	/// <summary>
	/// Evento disparado cuando cambia el estado de autenticación.
	/// </summary>
	event Action<User?>? OnAuthStateChanged;
}
