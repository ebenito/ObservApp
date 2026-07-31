namespace ObservApp.Shared.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservApp.Shared.Models;

public partial class HistorialViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<ObservationSession> sessions = new();

    [ObservableProperty]
    private ObservationSession? selectedSession;

    [RelayCommand]
    public async Task LoadSessions()
    {
        IsBusy = true;
        try
        {
            Sessions = new ObservableCollection<ObservationSession>();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    public void SelectSession(ObservationSession session) => SelectedSession = session;

    [RelayCommand]
    public void ClearSelection() => SelectedSession = null;
}
