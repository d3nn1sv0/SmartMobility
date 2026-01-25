namespace SmartMobilityApp.ViewModels;

[QueryProperty(nameof(Route), "Route")]
public partial class BusMapViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly ISignalRService _signalRService;
    private readonly BusAnimationService _animationService;

    [ObservableProperty]
    private RouteDto? _route;

    [ObservableProperty]
    private RouteDetailDto? _routeDetail;

    [ObservableProperty]
    private ObservableCollection<BusLocationDto> _busesOnRoute = new();

    [ObservableProperty]
    private BusLocationDto? _selectedBus;

    [ObservableProperty]
    private string _routeNumber = string.Empty;

    [ObservableProperty]
    private string? _routeName;

    [ObservableProperty]
    private ObservableCollection<StopEtaDto> _stopEtas = new();

    [ObservableProperty]
    private string? _latestNotification;

    [ObservableProperty]
    private bool _hasEtaData;

    [ObservableProperty]
    private bool _hasBusData;

    public event EventHandler? BusPositionsUpdated;

    public BusAnimationService AnimationService => _animationService;

    public BusMapViewModel(IApiService apiService, ISignalRService signalRService)
    {
        _apiService = apiService;
        _signalRService = signalRService;
        _animationService = new BusAnimationService();

        _signalRService.BusPositionUpdated += OnBusPositionUpdated;
        _signalRService.NextStopApproaching += OnNextStopApproaching;
        _animationService.PositionsUpdated += OnAnimationUpdated;
    }

    private void OnAnimationUpdated(object? sender, EventArgs e)
    {
        BusPositionsUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void OnBusPositionUpdated(object? sender, BusPositionUpdateDto position)
    {
        if (Route == null) return;

        var isOnThisRoute = position.RouteId == Route.Id ||
                            position.RouteName == Route.Name ||
                            position.RouteName == RouteName;

        if (isOnThisRoute)
        {
            _animationService.UpdateTargetPosition(
                position.BusId,
                position.BusNumber,
                position.RouteName,
                position.Latitude,
                position.Longitude,
                position.Speed,
                position.Heading,
                position.Timestamp);

            var existing = BusesOnRoute.FirstOrDefault(b => b.BusId == position.BusId);
            if (existing == null)
            {
                BusesOnRoute.Add(new BusLocationDto
                {
                    BusId = position.BusId,
                    BusNumber = position.BusNumber,
                    RouteName = position.RouteName,
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    Speed = position.Speed,
                    Heading = position.Heading,
                    Timestamp = position.Timestamp
                });
                HasBusData = true;
            }

            if (SelectedBus != null && SelectedBus.BusId == position.BusId)
            {
                _ = LoadEtaAsync();
            }
        }
    }

    private void OnNextStopApproaching(object? sender, NextStopNotificationDto notification)
    {
        if (Route == null) return;

        var busOnRoute = BusesOnRoute.FirstOrDefault(b => b.BusId == notification.BusId);
        if (busOnRoute != null)
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
    }

    partial void OnRouteChanged(RouteDto? value)
    {
        if (value != null)
        {
            RouteNumber = value.RouteNumber;
            RouteName = value.Name;
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Route == null) return;

        _animationService.Start();

        if (!_signalRService.IsConnected)
        {
            await _signalRService.ConnectAsync();
        }

        await _signalRService.SubscribeToAllBusesAsync();
        await LoadRouteDetailsAsync();
        await LoadBusesOnRouteAsync();
    }

    private async Task LoadRouteDetailsAsync()
    {
        if (Route == null) return;

        try
        {
            RouteDetail = await _apiService.GetAsync<RouteDetailDto>($"routes/{Route.Id}");

            if (RouteDetail?.Stops != null)
            {
                StopEtas.Clear();
                foreach (var stop in RouteDetail.Stops)
                {
                    StopEtas.Add(new StopEtaDto
                    {
                        StopId = stop.StopId,
                        StopName = stop.StopName,
                        StopOrder = stop.StopOrder,
                        EstimatedSeconds = (stop.EstimatedMinutesFromStart ?? 0) * 60,
                        IsNextStop = false
                    });
                }
                HasEtaData = StopEtas.Count > 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Route details load failed: {ex.Message}");
        }
    }

    private async Task LoadBusesOnRouteAsync()
    {
        if (Route == null) return;

        try
        {
            var allBuses = await _apiService.GetAsync<List<BusLocationDto>>("buspositions/all/latest");

            BusesOnRoute.Clear();
            _animationService.Clear();

            if (allBuses != null)
            {
                foreach (var bus in allBuses.Where(b => b.RouteName == Route.Name))
                {
                    BusesOnRoute.Add(bus);
                    _animationService.UpdateTargetPosition(
                        bus.BusId,
                        bus.BusNumber,
                        bus.RouteName,
                        bus.Latitude,
                        bus.Longitude,
                        bus.Speed,
                        bus.Heading,
                        bus.Timestamp);
                }
            }

            HasBusData = BusesOnRoute.Count > 0;

            if (BusesOnRoute.Count > 0 && SelectedBus == null)
            {
                SelectedBus = BusesOnRoute.First();
                await LoadEtaAsync();
            }

            BusPositionsUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Buses load failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadEtaAsync()
    {
        if (SelectedBus == null) return;

        try
        {
            var etaResponse = await _apiService.GetAsync<BusEtaResponseDto>($"buspositions/bus/{SelectedBus.BusId}/eta");

            if (etaResponse?.Stops != null)
            {
                StopEtas.Clear();
                foreach (var stop in etaResponse.Stops)
                {
                    StopEtas.Add(stop);
                }
                HasEtaData = StopEtas.Count > 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ETA load failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy || Route == null) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await LoadBusesOnRouteAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kunne ikke opdatere: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectBus(BusLocationDto bus)
    {
        if (bus == null) return;
        SelectedBus = bus;
        _ = LoadEtaAsync();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        _animationService.Stop();
        await _signalRService.UnsubscribeFromAllBusesAsync();
        await Shell.Current.GoToAsync("..");
    }
}
