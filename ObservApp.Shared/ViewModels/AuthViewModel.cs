namespace ObservApp.Shared.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;

/// <summary>
/// ViewModel para gestionar la autenticación del usuario.
/// Propiedades observables: Email, Password, IsLoading, ErrorMessage.
/// Comandos: SignIn, SignUp, SignOut.
/// </summary>
public partial class AuthViewModel : ObservableObject
{
	private readonly IAuthService _authService;
	private readonly AppState _appState;

	[ObservableProperty]
	private string email = string.Empty;

	[ObservableProperty]
	private string password = string.Empty;

	[ObservableProperty]
	private string displayName = string.Empty;

	[ObservableProperty]
	private bool isLoading;

	[ObservableProperty]
	private string? errorMessage;

	public AuthViewModel(IAuthService authService, AppState appState)
	{
		_authService = authService;
		_appState = appState;

		_authService.OnAuthStateChanged += OnAuthStateChanged;
	}

	[RelayCommand]
	public async Task SignInAsync()
	{
		try
		{
			IsLoading = true;
			ErrorMessage = null;

			var result = await _authService.SignInWithEmailAsync(Email, Password);
			if (!result.Success)
			{
				ErrorMessage = result.ErrorMessage ?? "Error al iniciar sesión";
				return;
			}

			_appState.CurrentUser = result.User;
			Email = string.Empty;
			Password = string.Empty;
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Error inesperado: {ex.Message}";
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	public async Task SignUpAsync()
	{
		try
		{
			IsLoading = true;
			ErrorMessage = null;

			if (string.IsNullOrWhiteSpace(DisplayName))
			{
				ErrorMessage = "El nombre mostrado es requerido";
				return;
			}

			var result = await _authService.SignUpWithEmailAsync(Email, Password, DisplayName);
			if (!result.Success)
			{
				ErrorMessage = result.ErrorMessage ?? "Error al registrarse";
				return;
			}

			_appState.CurrentUser = result.User;
			Email = string.Empty;
			Password = string.Empty;
			DisplayName = string.Empty;
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Error inesperado: {ex.Message}";
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	public async Task SignOutAsync()
	{
		try
		{
			IsLoading = true;
			ErrorMessage = null;

			await _authService.SignOutAsync();
			_appState.CurrentUser = null;
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Error al cerrar sesión: {ex.Message}";
		}
		finally
		{
			IsLoading = false;
		}
	}

	private void OnAuthStateChanged(UserProfile? userProfile)
	{
		if (userProfile != null)
		{
			_appState.CurrentUser = userProfile;
		}
	}
}
