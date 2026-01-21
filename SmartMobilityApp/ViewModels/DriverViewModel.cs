using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartMobilityApp.Models;
using SmartMobilityApp.Services;

namespace SmartMobilityApp.ViewModels;

public partial class DriverViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;
    private CancellationTokenSource? _trackingCts;

    [ObservableProperty]
    private bool _isTracking;

    [ObservableProperty]
    private double _currentLatitude;

    [ObservableProperty]
    private double _currentLongitude;

    [ObservableProperty]
    private double? _currentSpeed;

    [ObservableProperty]
    private string _statusText = "Tracking stoppet";

    [ObservableProperty]
    private bool _isLoadingBuses;

    [ObservableProperty]
    private BusDto? _selectedBus;

    public ObservableCollection<BusDto> AvailableBuses { get; } = new();

    public string UserName => _authService.CurrentUser?.Name ?? _authService.CurrentUser?.Email ?? "Chauffør";

    public bool CanStartTracking => SelectedBus != null && !IsTracking;

    public bool CanToggleTracking => IsTracking || SelectedBus != null;

    public DriverViewModel(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    partial void OnSelectedBusChanged(BusDto? value)
    {
        OnPropertyChanged(nameof(CanStartTracking));
        OnPropertyChanged(nameof(CanToggleTracking));
    }

    partial void OnIsTrackingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStartTracking));
        OnPropertyChanged(nameof(CanToggleTracking));
    }

    [RelayCommand]
    private async Task LoadBusesAsync()
    {
        if (IsLoadingBuses) return;

        try
        {
            IsLoadingBuses = true;
            ErrorMessage = null;

            var buses = await _apiService.GetAsync<List<BusDto>>("buses");

            AvailableBuses.Clear();

            if (buses != null && buses.Count > 0)
            {
                foreach (var bus in buses)
                {
                    AvailableBuses.Add(bus);
                }
            }
            else
            {
                ErrorMessage = "Ingen busser fundet. Kontakt administrator.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kunne ikke hente busser: {ex.Message}";
        }
        finally
        {
            IsLoadingBuses = false;
        }
    }

    [RelayCommand]
    private async Task ToggleTrackingAsync()
    {
        if (IsTracking)
        {
            await StopTrackingAsync();
        }
        else
        {
            await StartTrackingAsync();
        }
    }

    private async Task StartTrackingAsync()
    {
        if (SelectedBus == null)
        {
            ErrorMessage = "Vælg en bus før tracking kan startes";
            return;
        }

        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                ErrorMessage = "Lokationstilladelse er påkrævet for tracking";
                return;
            }

            IsTracking = true;
            StatusText = $"Tracking aktiv - Bus {SelectedBus.BusNumber}";
            ErrorMessage = null;

            _trackingCts = new CancellationTokenSource();
            _ = TrackingLoopAsync(_trackingCts.Token);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kunne ikke starte tracking: {ex.Message}";
            IsTracking = false;
            StatusText = "Tracking fejlede";
        }
    }

    private async Task StopTrackingAsync()
    {
        _trackingCts?.Cancel();
        _trackingCts?.Dispose();
        _trackingCts = null;

        IsTracking = false;
        StatusText = "Tracking stoppet";

        await Task.CompletedTask;
    }

    private async Task TrackingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.High,
                    Timeout = TimeSpan.FromSeconds(10)
                }, cancellationToken);

                if (location != null && SelectedBus != null)
                {
                    CurrentLatitude = location.Latitude;
                    CurrentLongitude = location.Longitude;
                    CurrentSpeed = location.Speed;

                    var positionDto = new CreateBusPositionDto
                    {
                        BusId = SelectedBus.Id,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Speed = location.Speed,
                        Heading = location.Course
                    };

                    var success = await _apiService.PostAsync("buspositions", positionDto);

                    StatusText = success
                        ? $"Bus {SelectedBus.BusNumber} - Sendt: {DateTime.Now:HH:mm:ss}"
                        : "Fejl ved afsendelse";
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusText = $"Fejl: {ex.Message}";
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await StopTrackingAsync();
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
