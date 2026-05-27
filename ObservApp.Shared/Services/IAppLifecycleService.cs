namespace ObservApp.Shared.Services;

public interface IAppLifecycleService
{
    bool CanExit { get; }
    Task ExitApplicationAsync();
}
