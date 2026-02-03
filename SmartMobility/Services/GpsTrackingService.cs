using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SmartMobility.Hubs;

namespace SmartMobility.Services;

public class GpsTrackingService : IGpsTrackingService
{
    private readonly SmartMobilityDbContext _context;
    private readonly IEtaService _etaService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GpsTrackingService> _logger;
    private readonly IHubContext<GpsTrackingHub> _hubContext;

    private static readonly ConcurrentDictionary<string, DateTime> NotifiedStops = new();
    private static readonly TimeSpan NotificationCooldown = TimeSpan.FromSeconds(Constants.GpsTracking.NotificationCooldownSeconds);

    private static readonly ConcurrentDictionary<int, CachedBusInfo> BusCache = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(Constants.GpsTracking.CacheExpiryMinutes);

    private static readonly Dictionary<string, int> DriverConnections = new();
    private static readonly Dictionary<int, string> ClaimedBuses = new();
    private static readonly object ConnectionLock = new();

    public GpsTrackingService(
        SmartMobilityDbContext context,
        IEtaService etaService,
        IServiceScopeFactory scopeFactory,
        ILogger<GpsTrackingService> logger,
        IHubContext<GpsTrackingHub> hubContext)
    {
        _context = context;
        _etaService = etaService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
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

    public async Task DriverGoOnlineAsync(string connectionId, int userId, UserRole role, int busId)
    {
        if (role != UserRole.Driver && role != UserRole.Admin)
        {
            _logger.LogWarning("Non-driver attempted GoOnline: UserId={UserId}, Role={Role}", userId, role);
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", new HubErrorDto
            {
                Code = "NOT_DRIVER",
                Message = "Kun chauffører kan gå online som bus"
            });
            return;
        }

        bool alreadyClaimed = false;
        lock (ConnectionLock)
        {
            if (DriverConnections.TryGetValue(connectionId, out var currentBusId))
            {
                if (currentBusId == busId)
                {
                    return;
                }

                ClaimedBuses.Remove(currentBusId);
                DriverConnections.Remove(connectionId);
            }

            if (ClaimedBuses.TryGetValue(busId, out var existingConnectionId))
            {
                _logger.LogWarning("Bus already claimed: BusId={BusId}, ExistingConnection={ExistingConnection}, AttemptedBy={ConnectionId}",
                    busId, existingConnectionId, connectionId);
                alreadyClaimed = true;
            }
            else
            {
                DriverConnections[connectionId] = busId;
                ClaimedBuses[busId] = connectionId;
            }
        }

        if (alreadyClaimed)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", new HubErrorDto
            {
                Code = "BUS_ALREADY_CLAIMED",
                Message = $"Bus {busId} er allerede taget af en anden chauffør"
            });
            return;
        }

        _logger.LogInformation("Driver went online: UserId={UserId}, ConnectionId={ConnectionId}, BusId={BusId}",
            userId, connectionId, busId);

        await _hubContext.Clients.Client(connectionId).SendAsync("OnlineSucceeded", new AuthenticationResultDto
        {
            Success = true,
            BusId = busId
        });
    }

    public async Task DriverGoOfflineAsync(string connectionId)
    {
        int? busId = null;
        lock (ConnectionLock)
        {
            if (DriverConnections.TryGetValue(connectionId, out var claimedBusId))
            {
                busId = claimedBusId;
                DriverConnections.Remove(connectionId);
                ClaimedBuses.Remove(claimedBusId);
            }
        }

        if (busId.HasValue)
        {
            _logger.LogInformation("Driver went offline: ConnectionId={ConnectionId}, BusId={BusId}",
                connectionId, busId.Value);
        }

        await _hubContext.Clients.Client(connectionId).SendAsync("OfflineSucceeded");
    }

    public Task HandleDisconnectAsync(string connectionId)
    {
        lock (ConnectionLock)
        {
            if (DriverConnections.TryGetValue(connectionId, out var busId))
            {
                DriverConnections.Remove(connectionId);
                ClaimedBuses.Remove(busId);
                _logger.LogInformation("Driver disconnected: ConnectionId={ConnectionId}, BusId={BusId}",
                    connectionId, busId);
            }
        }

        return Task.CompletedTask;
    }

    public async Task ProcessGpsUpdateAsync(string connectionId, GpsUpdateDto update)
    {
        int busId;
        bool isOnline;
        lock (ConnectionLock)
        {
            isOnline = DriverConnections.TryGetValue(connectionId, out busId);
        }

        if (!isOnline)
        {
            _logger.LogWarning("GPS update from offline driver: ConnectionId={ConnectionId}", connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", new HubErrorDto
            {
                Code = "NOT_ONLINE",
                Message = "Du skal gå online med en bus før du kan sende GPS opdateringer"
            });
            return;
        }

        var result = await CreatePositionUpdateAsync(busId, update);

        if (result?.PositionUpdate == null)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", new HubErrorDto
            {
                Code = "BUS_NOT_FOUND",
                Message = "Den tilknyttede bus blev ikke fundet"
            });
            return;
        }

        await _hubContext.Clients.Group($"{Constants.SignalRGroups.SubscribersBusPrefix}{busId}").SendAsync("BusPositionUpdated", result.PositionUpdate);
        await _hubContext.Clients.Group(Constants.SignalRGroups.SubscribersAll).SendAsync("BusPositionUpdated", result.PositionUpdate);

        if (result.NextStopNotification != null)
        {
            await _hubContext.Clients.Group($"{Constants.SignalRGroups.SubscribersBusPrefix}{busId}").SendAsync("NextStopApproaching", result.NextStopNotification);
            await _hubContext.Clients.Group(Constants.SignalRGroups.SubscribersAll).SendAsync("NextStopApproaching", result.NextStopNotification);

            _logger.LogInformation("Next stop notification sent: BusId={BusId}, StopId={StopId}, StopName={StopName}",
                busId, result.NextStopNotification.StopId, result.NextStopNotification.StopName);
        }

        SavePositionInBackground(busId, update);

        _logger.LogDebug("GPS update processed: BusId={BusId}, Lat={Lat}, Lng={Lng}",
            busId, update.Latitude, update.Longitude);
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
