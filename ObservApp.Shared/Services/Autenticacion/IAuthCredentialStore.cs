namespace ObservApp.Shared.Services;

/// <summary>
/// Contrato para persistencia segura de credenciales del usuario.
/// </summary>
public interface IAuthCredentialStore
{
	Task SaveAsync(string email, string password);
	Task<(string? Email, string? Password)> LoadAsync();
	Task ClearAsync();
}
