using Microsoft.AspNetCore.SignalR;

namespace SmartMobility.Hubs;

[Authorize]
public class GpsTrackingHub : Hub
{
    private readonly IGpsTrackingService _gpsTrackingService;
    private readonly ILogger<GpsTrackingHub> _logger;

    private static readonly Dictionary<string, int> DriverConnections = new();
    private static readonly Dictionary<int, string> ClaimedBuses = new();
    private static readonly object ConnectionLock = new();

    public GpsTrackingHub(
        IGpsTrackingService gpsTrackingService,
        ILogger<GpsTrackingHub> logger)
    {
        _gpsTrackingService = gpsTrackingService;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();

        lock (ConnectionLock)
        {
            if (DriverConnections.TryGetValue(connectionId, out var busId))
            {
                DriverConnections.Remove(connectionId);
                ClaimedBuses.Remove(busId);
                _logger.LogInformation("Driver disconnected: UserId={UserId}, ConnectionId={ConnectionId}, BusId={BusId}",
                    userId, connectionId, busId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task GoOnline(int busId)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();
        var role = GetUserRole();

        if (role != UserRole.Driver && role != UserRole.Admin)
        {
            _logger.LogWarning("Non-driver attempted GoOnline: UserId={UserId}, Role={Role}", userId, role);
            await Clients.Caller.SendAsync("Error", new HubErrorDto
            {
                Code = "NOT_DRIVER",
                Message = "Kun chauffører kan gå online som bus"
            });
            return;
        }

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

                Clients.Caller.SendAsync("Error", new HubErrorDto
                {
                    Code = "BUS_ALREADY_CLAIMED",
                    Message = $"Bus {busId} er allerede taget af en anden chauffør"
                }).Wait();
                return;
            }

            DriverConnections[connectionId] = busId;
            ClaimedBuses[busId] = connectionId;
        }

        await Groups.AddToGroupAsync(connectionId, $"{Constants.SignalRGroups.BusPrefix}{busId}");

        _logger.LogInformation("Driver went online: UserId={UserId}, ConnectionId={ConnectionId}, BusId={BusId}",
            userId, connectionId, busId);

        await Clients.Caller.SendAsync("OnlineSucceeded", new AuthenticationResultDto
        {
            Success = true,
            BusId = busId
        });
    }

    public async Task GoOffline()
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();

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
            await Groups.RemoveFromGroupAsync(connectionId, $"{Constants.SignalRGroups.BusPrefix}{busId.Value}");
            _logger.LogInformation("Driver went offline: UserId={UserId}, ConnectionId={ConnectionId}, BusId={BusId}",
                userId, connectionId, busId.Value);
        }

        await Clients.Caller.SendAsync("OfflineSucceeded");
    }

    public async Task SendGpsUpdate(double latitude, double longitude, double? speed = null, double? heading = null)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();

        int busId;
        bool isOnline;
        lock (ConnectionLock)
        {
            isOnline = DriverConnections.TryGetValue(connectionId, out busId);
        }

        if (!isOnline)
        {
            _logger.LogWarning("GPS update from offline driver: UserId={UserId}, ConnectionId={ConnectionId}", userId, connectionId);
            await Clients.Caller.SendAsync("Error", new HubErrorDto
            {
                Code = "NOT_ONLINE",
                Message = "Du skal gå online med en bus før du kan sende GPS opdateringer"
            });
            return;
        }

        var update = new GpsUpdateDto
        {
            Latitude = latitude,
            Longitude = longitude,
            Speed = speed,
            Heading = heading
        };

        var result = await _gpsTrackingService.CreatePositionUpdateAsync(busId, update);

        if (result?.PositionUpdate == null)
        {
            await Clients.Caller.SendAsync("Error", new HubErrorDto
            {
                Code = "BUS_NOT_FOUND",
                Message = "Den tilknyttede bus blev ikke fundet"
            });
            return;
        }

        await Clients.Group($"{Constants.SignalRGroups.SubscribersBusPrefix}{busId}").SendAsync("BusPositionUpdated", result.PositionUpdate);
        await Clients.Group(Constants.SignalRGroups.SubscribersAll).SendAsync("BusPositionUpdated", result.PositionUpdate);

        if (result.NextStopNotification != null)
        {
            await Clients.Group($"{Constants.SignalRGroups.SubscribersBusPrefix}{busId}").SendAsync("NextStopApproaching", result.NextStopNotification);
            await Clients.Group(Constants.SignalRGroups.SubscribersAll).SendAsync("NextStopApproaching", result.NextStopNotification);

            _logger.LogInformation("Next stop notification sent: BusId={BusId}, StopId={StopId}, StopName={StopName}",
                busId, result.NextStopNotification.StopId, result.NextStopNotification.StopName);
        }

        _gpsTrackingService.SavePositionInBackground(busId, update);

        _logger.LogDebug("GPS update processed: UserId={UserId}, BusId={BusId}, Lat={Lat}, Lng={Lng}",
            userId, busId, latitude, longitude);
    }

    public async Task SubscribeToBus(int busId)
    {
        var connectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(connectionId, $"{Constants.SignalRGroups.SubscribersBusPrefix}{busId}");
        _logger.LogInformation("Client subscribed to bus: UserId={UserId}, ConnectionId={ConnectionId}, BusId={BusId}",
            GetUserId(), connectionId, busId);
    }

    public async Task UnsubscribeFromBus(int busId)
    {
        var connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, $"{Constants.SignalRGroups.SubscribersBusPrefix}{busId}");
        _logger.LogInformation("Client unsubscribed from bus: UserId={UserId}, ConnectionId={ConnectionId}, BusId={BusId}",
            GetUserId(), connectionId, busId);
    }

    public async Task SubscribeToAllBuses()
    {
        var connectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(connectionId, Constants.SignalRGroups.SubscribersAll);
        _logger.LogInformation("Client subscribed to all buses: UserId={UserId}, ConnectionId={ConnectionId}",
            GetUserId(), connectionId);
    }

    public async Task UnsubscribeFromAllBuses()
    {
        var connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, Constants.SignalRGroups.SubscribersAll);
        _logger.LogInformation("Client unsubscribed from all buses: UserId={UserId}, ConnectionId={ConnectionId}",
            GetUserId(), connectionId);
    }

    private int GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirst("sub");

        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return 0;
    }

    private UserRole GetUserRole()
    {
        var roleClaim = Context.User?.FindFirst(ClaimTypes.Role);
        if (roleClaim != null && Enum.TryParse<UserRole>(roleClaim.Value, out var role))
        {
            return role;
        }

        return UserRole.User;
    }
}
