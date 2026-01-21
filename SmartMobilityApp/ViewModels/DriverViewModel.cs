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
    private int _assignedBusId = 1;

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

    public string UserName => _authService.CurrentUser?.Name ?? _authService.CurrentUser?.Email ?? "Chauffør";

    public DriverViewModel(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
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
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                ErrorMessage = "Lokationstilladelse er påkrævet for tracking";
                return;
            }

            IsTracking = true;
            StatusText = "Tracking aktiv";
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

                if (location != null)
                {
                    CurrentLatitude = location.Latitude;
                    CurrentLongitude = location.Longitude;
                    CurrentSpeed = location.Speed;

                    var positionDto = new CreateBusPositionDto
                    {
                        BusId = _assignedBusId,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Speed = location.Speed,
                        Heading = location.Course
                    };

                    var success = await _apiService.PostAsync("/buspositions", positionDto);

                    StatusText = success
                        ? $"Sendt: {DateTime.Now:HH:mm:ss}"
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
