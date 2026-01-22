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
    private readonly ISignalRService _signalRService;

    [ObservableProperty]
    private ObservableCollection<RouteDto> _routes = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string? _latestNotification;

    [ObservableProperty]
    private bool _isConnected;

    public string UserName => _authService.CurrentUser?.Name ?? _authService.CurrentUser?.Email ?? "Passager";

    public PassengerViewModel(IApiService apiService, IAuthService authService, ISignalRService signalRService)
    {
        _apiService = apiService;
        _authService = authService;
        _signalRService = signalRService;

        _signalRService.NextStopApproaching += OnNextStopApproaching;
        _signalRService.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private void OnNextStopApproaching(object? sender, NextStopNotificationDto notification)
    {
        LatestNotification = $"Bus {notification.BusNumber} nærmer sig {notification.StopName}";

        Task.Delay(10000).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (LatestNotification?.Contains(notification.StopName) == true)
                {
                    LatestNotification = null;
                }
            });
        });
    }

    private void OnConnectionStateChanged(object? sender, string state)
    {
        IsConnected = state == "Connected";
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await _signalRService.ConnectAsync();
        await _signalRService.SubscribeToAllBusesAsync();
        await RefreshAsync();
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

            var routes = await _apiService.GetAsync<List<RouteDto>>("routes");

            Routes.Clear();

            if (routes != null)
            {
                foreach (var route in routes.Where(r => r.IsActive))
                {
                    Routes.Add(route);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kunne ikke hente ruter: {ex.Message}";
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

    [RelayCommand]
    private async Task SelectRouteAsync(RouteDto route)
    {
        if (route == null) return;

        await Shell.Current.GoToAsync("BusMapPage", new Dictionary<string, object>
        {
            ["Route"] = route
        });
    }
}
