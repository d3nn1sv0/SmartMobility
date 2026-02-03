namespace SmartMobilityApp.ViewModels;

public partial class DriverViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;
    private HubConnection? _hubConnection;

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
                .WithUrl(Configuration.Constants.Api.HubUrl, options =>
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

            _hubConnection.Reconnecting += error =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusText = "Forbindelse tabt - genopretter...";
                });
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += async connectionId =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusText = "Forbundet - går online igen...";
                });

                if (SelectedBus != null)
                {
                    await _hubConnection.InvokeAsync("GoOnline", SelectedBus.Id);
                }
            };

            _hubConnection.Closed += error =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusText = "Forbindelse lukket";
                    IsTracking = false;
                });
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("GoOnline", SelectedBus.Id);

            IsTracking = true;
            StatusText = $"Tracking aktiv - Bus {SelectedBus.BusNumber}";
            ErrorMessage = null;

            await StartLocationListeningAsync();
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
        StopLocationListening();

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

    private async Task StartLocationListeningAsync()
    {
        Geolocation.LocationChanged += OnLocationChanged;

        var success = await Geolocation.StartListeningForegroundAsync(new GeolocationListeningRequest
        {
            DesiredAccuracy = GeolocationAccuracy.Best,
            MinimumTime = TimeSpan.FromMilliseconds(Configuration.Constants.Geolocation.MinimumTimeMs)
        });

        if (!success)
        {
            ErrorMessage = "Kunne ikke starte GPS listening";
        }
    }

    private async void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var location = e.Location;

        if (location != null && SelectedBus != null && _hubConnection?.State == HubConnectionState.Connected)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentLatitude = location.Latitude;
                CurrentLongitude = location.Longitude;
                CurrentSpeed = location.Speed;
                StatusText = $"Bus {SelectedBus.BusNumber} - Sendt: {DateTime.Now:HH:mm:ss}";
            });

            try
            {
                await _hubConnection.InvokeAsync("SendGpsUpdate",
                    location.Latitude,
                    location.Longitude,
                    location.Speed,
                    location.Course);
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusText = $"Fejl: {ex.Message}";
                });
            }
        }
    }

    private void StopLocationListening()
    {
        Geolocation.LocationChanged -= OnLocationChanged;
        Geolocation.StopListeningForeground();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await StopTrackingAsync();
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
