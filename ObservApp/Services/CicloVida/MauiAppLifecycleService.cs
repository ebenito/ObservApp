using ObservApp.Shared.Services;

namespace ObservApp.Services;

public class MauiAppLifecycleService : IAppLifecycleService
{
    public bool CanExit => true;

    public Task ExitApplicationAsync()
    {
#if WINDOWS
        Microsoft.UI.Xaml.Application.Current?.Exit();
#else
        Application.Current?.Quit();
#endif
        return Task.CompletedTask;
    }
}
