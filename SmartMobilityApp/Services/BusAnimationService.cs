namespace SmartMobilityApp.Services;

public class AnimatedBusPosition
{
    public int BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string? RouteName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TargetLatitude { get; set; }
    public double TargetLongitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
}

public class BusAnimationService : IBusAnimationService
{
    private readonly Dictionary<int, AnimatedBusPosition> _buses = new();
    private IDispatcherTimer? _timer;
    private const double AnimationStepMs = 50;
    private const double InterpolationSpeed = 0.15;

    public event EventHandler? PositionsUpdated;

    public IReadOnlyCollection<AnimatedBusPosition> Buses => _buses.Values;

    public void Start()
    {
        if (_timer != null) return;

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null) return;

        _timer.Interval = TimeSpan.FromMilliseconds(AnimationStepMs);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        _buses.Clear();
    }

    public void UpdateTargetPosition(int busId, string busNumber, string? routeName,
        double latitude, double longitude, double? speed, double? heading, DateTime timestamp)
    {
        if (_buses.TryGetValue(busId, out var existing))
        {
            existing.TargetLatitude = latitude;
            existing.TargetLongitude = longitude;
            existing.Speed = speed;
            existing.Heading = heading;
            existing.Timestamp = timestamp;
        }
        else
        {
            _buses[busId] = new AnimatedBusPosition
            {
                BusId = busId,
                BusNumber = busNumber,
                RouteName = routeName,
                Latitude = latitude,
                Longitude = longitude,
                TargetLatitude = latitude,
                TargetLongitude = longitude,
                Speed = speed,
                Heading = heading,
                Timestamp = timestamp
            };
        }
    }

    public void RemoveBus(int busId)
    {
        _buses.Remove(busId);
    }

    public void Clear()
    {
        _buses.Clear();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        bool anyMoved = false;

        foreach (var bus in _buses.Values)
        {
            var latDiff = bus.TargetLatitude - bus.Latitude;
            var lonDiff = bus.TargetLongitude - bus.Longitude;

            if (Math.Abs(latDiff) > 0.000001 || Math.Abs(lonDiff) > 0.000001)
            {
                bus.Latitude += latDiff * InterpolationSpeed;
                bus.Longitude += lonDiff * InterpolationSpeed;
                anyMoved = true;
            }
        }

        if (anyMoved)
        {
            PositionsUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
