using Microsoft.AspNetCore.SignalR;

namespace SmartMobility.Hubs;

[Authorize]
public class GpsTrackingHub : Hub
{
    private readonly IGpsTrackingService _gpsTrackingService;
    private readonly ILogger<GpsTrackingHub> _logger;

    public GpsTrackingHub(
        IGpsTrackingService gpsTrackingService,
        ILogger<GpsTrackingHub> logger)
    {
        _gpsTrackingService = gpsTrackingService;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _gpsTrackingService.HandleDisconnectAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task GoOnline(int busId)
    {
        await _gpsTrackingService.DriverGoOnlineAsync(
            Context.ConnectionId,
            GetUserId(),
            GetUserRole(),
            busId);
    }

    public async Task GoOffline()
    {
        await _gpsTrackingService.DriverGoOfflineAsync(Context.ConnectionId);
    }

    public async Task SendGpsUpdate(double latitude, double longitude, double? speed = null, double? heading = null)
    {
        var update = new GpsUpdateDto
        {
            Latitude = latitude,
            Longitude = longitude,
            Speed = speed,
            Heading = heading
        };

        await _gpsTrackingService.ProcessGpsUpdateAsync(Context.ConnectionId, update);
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
