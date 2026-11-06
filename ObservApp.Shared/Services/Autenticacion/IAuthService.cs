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
	Task<bool> LoginAsync(string email, string password, bool rememberCredentials = false);

	/// <summary>
	/// Verifica el OTP de registro enviado por email.
	/// </summary>
	Task<bool> VerifyOtpAsync(string email, string code);

	/// <summary>
	/// Envía al email un código de recuperación de contraseña (plantilla
	/// "Reset Password" de Supabase con <c>{{ .Token }}</c>).
	/// </summary>
	Task<bool> RequestPasswordResetAsync(string email);

	/// <summary>
	/// Verifica el código de recuperación y abre una sesión temporal con la
	/// que se puede cambiar la contraseña.
	/// </summary>
	Task<bool> VerifyRecoveryOtpAsync(string email, string code);

	/// <summary>
	/// Cambia la contraseña del usuario de la sesión actual.
	/// </summary>
	Task<bool> UpdatePasswordAsync(string newPassword);

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
