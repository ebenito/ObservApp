namespace ObservApp.Shared.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Localization;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using Supabase.Gotrue;

/// <summary>
/// ViewModel para gestionar la autenticación del usuario.
/// Propiedades observables: Email, Password, IsLoading, ErrorMessage.
/// Comandos: SignIn, SignUp, SignOut.
/// </summary>
public partial class AuthViewModel : ObservableObject
{
	private readonly IAuthService _authService;
	private readonly AppState _appState;
	private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

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

	public AuthViewModel(
		IAuthService authService,
		AppState appState,
		IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
	{
		_authService = authService;
		_appState = appState;
		_l = localizer;

		_authService.OnAuthStateChanged += OnAuthStateChanged;
	}

	[RelayCommand]
	public async Task SignInAsync()
	{
		try
		{
			IsLoading = true;
			ErrorMessage = null;

			var ok = await _authService.LoginAsync(Email, Password);
			if (!ok)
			{
				ErrorMessage = _authService.LastError ?? _l["Auth_SignIn_Error"];
				return;
			}

			_appState.CurrentUser = _authService.CurrentUser;
			Email = string.Empty;
			Password = string.Empty;
		}
		catch (Exception ex)
		{
			ErrorMessage = string.Format(_l["Error_Unexpected"], ex.Message);
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

			var ok = await _authService.SignUpAsync(Email, Password);
			if (!ok)
			{
				ErrorMessage = _authService.LastError ?? _l["Auth_SignUp_Error"];
				return;
			}

			_appState.CurrentUser = _authService.CurrentUser;
			Email = string.Empty;
			Password = string.Empty;
			DisplayName = string.Empty;
		}
		catch (Exception ex)
		{
			ErrorMessage = string.Format(_l["Error_Unexpected"], ex.Message);
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

			await _authService.LogoutAsync();
			_appState.CurrentUser = null;
		}
		catch (Exception ex)
		{
			ErrorMessage = string.Format(_l["Auth_SignOut_Error"], ex.Message);
		}
		finally
		{
			IsLoading = false;
		}
	}

	private void OnAuthStateChanged(User? user)
	{
		_appState.CurrentUser = user;
	}
}
