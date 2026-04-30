namespace ObservApp.Shared.State;

public class AppState
{
    private string _theme = "dark";
    private bool _isAuthenticated;
    private string _userDisplayName = string.Empty;

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
}
