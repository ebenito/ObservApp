namespace ObservApp.Shared.State;

using Supabase.Gotrue;

public class AppState
{
    private string _theme = "dark";
    private bool _isAuthenticated;
    private string _userDisplayName = string.Empty;
    private User? _currentUser;

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
    /// Usuario autenticado de Supabase.
    /// Al asignarse, actualiza automáticamente IsAuthenticated y UserDisplayName.
    /// </summary>
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            _isAuthenticated = value != null;
            _userDisplayName = ResolveDisplayName(value);
            OnAuthChanged?.Invoke();
#pragma warning disable CS0618
            OnStateChanged?.Invoke();
#pragma warning restore CS0618
        }
    }

    private static string ResolveDisplayName(User? user)
    {
        if (user is null)
        {
            return string.Empty;
        }

        if (user.UserMetadata?.TryGetValue("display_name", out var displayName) == true)
        {
            var value = displayName?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return user.Email ?? string.Empty;
    }
}