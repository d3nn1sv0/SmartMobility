using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartMobilityApp.Models;
using SmartMobilityApp.Services;

namespace SmartMobilityApp.ViewModels;

public partial class PassengerViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ObservableCollection<BusLocationDto> _busPositions = new();

    [ObservableProperty]
    private bool _isRefreshing;

    public string UserName => _authService.CurrentUser?.Name ?? _authService.CurrentUser?.Email ?? "Passager";

    public PassengerViewModel(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;
            ErrorMessage = null;

            var positions = await _apiService.GetAsync<List<BusLocationDto>>("buspositions/all/latest");

            BusPositions.Clear();

            if (positions != null)
            {
                foreach (var position in positions)
                {
                    BusPositions.Add(position);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kunne ikke hente buspositioner: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
