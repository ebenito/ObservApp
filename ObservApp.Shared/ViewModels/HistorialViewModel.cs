namespace ObservApp.Shared.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservApp.Shared.Models;
using ObservApp.Shared.Services;

public partial class HistorialViewModel : BaseViewModel
{
    private readonly IObservationService _observationService;

    [ObservableProperty]
    private ObservableCollection<ObservationSession> sessions = new();

    [ObservableProperty]
    private ObservationSession? selectedSession;

    public HistorialViewModel(IObservationService observationService)
    {
        _observationService = observationService;
    }

    [RelayCommand]
    public async Task LoadSessionsAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var list = await _observationService.GetAllAsync(cancellationToken);
            Sessions = new ObservableCollection<ObservationSession>(
                list.OrderByDescending(s => s.Date));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSessionAsync(ObservationSession session)
    {
        if (session is null) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var ok = await _observationService.DeleteAsync(session.Id);
            if (ok)
                Sessions.Remove(session);
            else
                ErrorMessage = _observationService.LastError;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void SelectSession(ObservationSession session) => SelectedSession = session;

    [RelayCommand]
    public void ClearSelection() => SelectedSession = null;
}
