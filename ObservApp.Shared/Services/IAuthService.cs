namespace ObservApp.Shared.Services;

/// <summary>
/// Resultado de una operación de autenticación.
/// </summary>
public record AuthResult(bool Success, string? ErrorMessage, UserProfile? User);

/// <summary>
/// Perfil de usuario autenticado.
/// </summary>
public record UserProfile(string Id, string Email, string? DisplayName);

/// <summary>
/// Interfaz para servicios de autenticación.
/// Implementaciones: SupabaseService (MAUI/Web.Client), SsrAuthService (Web SSR).
/// </summary>
public interface IAuthService
{
	/// <summary>
	/// Inicia sesión con email y contraseña.
	/// </summary>
	Task<AuthResult> SignInWithEmailAsync(string email, string password);

	/// <summary>
	/// Registra un nuevo usuario con email, contraseña y nombre mostrado.
	/// </summary>
	Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName);

	/// <summary>
	/// Cierra la sesión del usuario actual.
	/// </summary>
	Task SignOutAsync();

	/// <summary>
	/// Obtiene el perfil del usuario autenticado.
	/// </summary>
	Task<UserProfile?> GetCurrentUserAsync();

	/// <summary>
	/// Verifica si hay un usuario autenticado.
	/// </summary>
	Task<bool> IsAuthenticatedAsync();

	/// <summary>
	/// Evento disparado cuando cambia el estado de autenticación.
	/// </summary>
	event Action<UserProfile?>? OnAuthStateChanged;
}
