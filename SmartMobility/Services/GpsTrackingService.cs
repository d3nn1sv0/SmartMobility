using System.Collections.Concurrent;

namespace SmartMobility.Services;

public class GpsTrackingService : IGpsTrackingService
{
    private readonly SmartMobilityDbContext _context;
    private readonly IEtaService _etaService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GpsTrackingService> _logger;
    private static readonly ConcurrentDictionary<string, DateTime> NotifiedStops = new();
    private static readonly TimeSpan NotificationCooldown = TimeSpan.FromMinutes(Constants.GpsTracking.NotificationCooldownMinutes);

    private static readonly ConcurrentDictionary<int, CachedBusInfo> BusCache = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(Constants.GpsTracking.CacheExpiryMinutes);

    public GpsTrackingService(
        SmartMobilityDbContext context,
        IEtaService etaService,
        IServiceScopeFactory scopeFactory,
        ILogger<GpsTrackingService> logger)
    {
        _context = context;
        _etaService = etaService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public static void InvalidateBusCache(int busId)
    {
        BusCache.TryRemove(busId, out _);
    }

    public static void InvalidateAllBusCache()
    {
        BusCache.Clear();
    }

    public async Task<GpsProcessingResult?> CreatePositionUpdateAsync(int busId, GpsUpdateDto update)
    {
        var cachedBus = await GetOrLoadBusAsync(busId);

        if (cachedBus == null)
            return null;

        var positionUpdate = new BusPositionUpdateDto
        {
            BusId = cachedBus.BusId,
            BusNumber = cachedBus.BusNumber,
            RouteId = cachedBus.CurrentRouteId,
            RouteName = cachedBus.RouteName,
            Latitude = update.Latitude,
            Longitude = update.Longitude,
            Speed = update.Speed,
            Heading = update.Heading,
            Timestamp = DateTime.UtcNow
        };

        var result = new GpsProcessingResult
        {
            PositionUpdate = positionUpdate
        };

        if (cachedBus.RouteStops.Count > 0)
        {
            var notification = CheckForApproachingStop(cachedBus, update.Latitude, update.Longitude);
            if (notification != null)
            {
                result.NextStopNotification = notification;
            }
        }

        return result;
    }

    public void SavePositionInBackground(int busId, GpsUpdateDto update)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SmartMobilityDbContext>();

                var position = new BusPosition
                {
                    BusId = busId,
                    Latitude = update.Latitude,
                    Longitude = update.Longitude,
                    Speed = update.Speed,
                    Heading = update.Heading,
                    Timestamp = DateTime.UtcNow
                };

                context.BusPositions.Add(position);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save GPS position for bus {BusId}", busId);
            }
        });
    }

    private async Task<CachedBusInfo?> GetOrLoadBusAsync(int busId)
    {
        if (BusCache.TryGetValue(busId, out var cached) && DateTime.UtcNow - cached.LoadedAt < CacheExpiry)
        {
            return cached;
        }

        var bus = await _context.Buses
            .Include(b => b.CurrentRoute)
                .ThenInclude(r => r!.RouteStops)
                    .ThenInclude(rs => rs.Stop)
            .FirstOrDefaultAsync(b => b.Id == busId);

        if (bus == null)
            return null;

        var busInfo = new CachedBusInfo
        {
            BusId = bus.Id,
            BusNumber = bus.BusNumber,
            CurrentRouteId = bus.CurrentRouteId,
            RouteName = bus.CurrentRoute?.Name,
            RouteStops = bus.CurrentRoute?.RouteStops?
                .OrderBy(rs => rs.StopOrder)
                .Select(rs => new CachedRouteStop
                {
                    StopId = rs.StopId,
                    StopName = rs.Stop.Name,
                    Latitude = rs.Stop.Latitude,
                    Longitude = rs.Stop.Longitude,
                    StopOrder = rs.StopOrder
                })
                .ToList() ?? new List<CachedRouteStop>(),
            LoadedAt = DateTime.UtcNow
        };

        BusCache[busId] = busInfo;
        return busInfo;
    }

    private NextStopNotificationDto? CheckForApproachingStop(CachedBusInfo bus, double busLat, double busLon)
    {
        if (bus.RouteStops.Count == 0)
            return null;

        foreach (var routeStop in bus.RouteStops)
        {
            var distance = _etaService.CalculateDistanceMeters(
                busLat, busLon,
                routeStop.Latitude, routeStop.Longitude);

            if (distance <= Constants.GpsTracking.NotificationDistanceMeters)
            {
                var notificationKey = $"{bus.BusId}-{routeStop.StopId}";

                if (NotifiedStops.TryGetValue(notificationKey, out var lastNotified))
                {
                    if (DateTime.UtcNow - lastNotified < NotificationCooldown)
                    {
                        continue;
                    }
                }

                NotifiedStops[notificationKey] = DateTime.UtcNow;

                CleanupOldNotifications();

                var estimatedSeconds = (int)Math.Ceiling((distance / 1000.0) / 30.0 * 3600);

                return new NextStopNotificationDto
                {
                    BusId = bus.BusId,
                    BusNumber = bus.BusNumber,
                    StopId = routeStop.StopId,
                    StopName = routeStop.StopName,
                    EstimatedSeconds = estimatedSeconds,
                    DistanceMeters = distance
                };
            }
        }

        return null;
    }

    private static void CleanupOldNotifications()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(Constants.GpsTracking.NotificationCleanupMinutes);
        var keysToRemove = NotifiedStops
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            NotifiedStops.TryRemove(key, out _);
        }
    }

    private class CachedBusInfo
    {
        public int BusId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public int? CurrentRouteId { get; set; }
        public string? RouteName { get; set; }
        public List<CachedRouteStop> RouteStops { get; set; } = new();
        public DateTime LoadedAt { get; set; }
    }

    private class CachedRouteStop
    {
        public int StopId { get; set; }
        public string StopName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int StopOrder { get; set; }
    }
}
