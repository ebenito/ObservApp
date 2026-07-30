namespace ObservApp.Shared.State;

using ObservApp.Shared.Services;

public class AppState
{
    private string _theme = "dark";
    private bool _isAuthenticated;
    private string _userDisplayName = string.Empty;
    private UserProfile? _currentUser;

    public event Action? OnStateChanged;

    public string Theme
    {
        get => _theme;
        set { _theme = value; OnStateChanged?.Invoke(); }
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set { _isAuthenticated = value; OnStateChanged?.Invoke(); }
    }

    public string UserDisplayName
    {
        get => _userDisplayName;
        set { _userDisplayName = value; OnStateChanged?.Invoke(); }
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
            IsAuthenticated = value != null;
            UserDisplayName = value?.DisplayName ?? string.Empty;
            OnStateChanged?.Invoke();
        }
    }
}
