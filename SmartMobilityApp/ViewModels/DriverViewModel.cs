namespace SmartMobilityApp.ViewModels;

public partial class DriverViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;
    private HubConnection? _hubConnection;
    private CancellationTokenSource? _trackingCts;
    private const string HubUrl = "http://10.0.2.2:5174/hubs/gpstracking";

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

            StatusText = "Forbinder til server...";

            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Ikke logget ind";
                return;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(HubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<object>("OnlineSucceeded", _ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusText = $"Online - Bus {SelectedBus.BusNumber}";
                });
            });

            _hubConnection.On<object>("Error", error =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ErrorMessage = $"Server fejl: {error}";
                });
            });

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("GoOnline", SelectedBus.Id);

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

        if (_hubConnection != null)
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("GoOffline");
                }
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            catch { }
            _hubConnection = null;
        }

        IsTracking = false;
        StatusText = "Tracking stoppet";
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

                if (location != null && SelectedBus != null && _hubConnection?.State == HubConnectionState.Connected)
                {
                    CurrentLatitude = location.Latitude;
                    CurrentLongitude = location.Longitude;
                    CurrentSpeed = location.Speed;

                    await _hubConnection.InvokeAsync("SendGpsUpdate",
                        location.Latitude,
                        location.Longitude,
                        location.Speed,
                        location.Course,
                        cancellationToken);

                    StatusText = $"Bus {SelectedBus.BusNumber} - Sendt: {DateTime.Now:HH:mm:ss}";
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
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
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
