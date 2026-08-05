namespace ObservApp.Shared.State;

using ObservApp.Shared.Services;

public class AppState
{
    private string _theme = "dark";
    private bool _isAuthenticated;
    private string _userDisplayName = string.Empty;
    private UserProfile? _currentUser;

    /// <summary>
    /// Evento genérico disparado en cualquier cambio de estado.
    /// </summary>
    [Obsolete("Usa OnThemeChanged u OnAuthChanged para suscripciones granulares.")]
    public event Action? OnStateChanged;

    /// <summary>
    /// Evento disparado cuando cambia el tema (claro/oscuro).
    /// </summary>
    public event Action? OnThemeChanged;

    /// <summary>
    /// Evento disparado cuando cambia el estado de autenticación o el usuario.
    /// </summary>
    public event Action? OnAuthChanged;

    public string Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            OnThemeChanged?.Invoke();
#pragma warning disable CS0618
            OnStateChanged?.Invoke();
#pragma warning restore CS0618
        }
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set
        {
            _isAuthenticated = value;
            OnAuthChanged?.Invoke();
#pragma warning disable CS0618
            OnStateChanged?.Invoke();
#pragma warning restore CS0618
        }
    }

    public string UserDisplayName
    {
        get => _userDisplayName;
        set
        {
            _userDisplayName = value;
            OnAuthChanged?.Invoke();
#pragma warning disable CS0618
            OnStateChanged?.Invoke();
#pragma warning restore CS0618
        }
    }

    /// <summary>
    /// Perfil del usuario autenticado.
    /// Al asignarse, actualiza automáticamente IsAuthenticated y UserDisplayName.
    /// </summary>
    public UserProfile? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            _isAuthenticated = value != null;
            _userDisplayName = value?.DisplayName ?? string.Empty;
            OnAuthChanged?.Invoke();
#pragma warning disable CS0618
            OnStateChanged?.Invoke();
#pragma warning restore CS0618
        }
    }
}