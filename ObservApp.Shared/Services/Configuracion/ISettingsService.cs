namespace ObservApp.Shared.Services;

public interface ISettingsService
{
    string GetLanguage();
    void SetLanguage(string languageCode);
    string GetTheme();
    void SetTheme(string theme);
}
