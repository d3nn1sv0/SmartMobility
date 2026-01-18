using Microsoft.AspNetCore.SignalR;
using SmartMobility.DTOs;
using SmartMobility.Services.Interfaces;

namespace SmartMobility.Hubs;

public class GpsTrackingHub : Hub
{
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly IGpsTrackingService _gpsTrackingService;
    private readonly ILogger<GpsTrackingHub> _logger;

    private static readonly Dictionary<string, (int BusId, string Token)> AuthenticatedConnections = new();
    private static readonly object ConnectionLock = new();

    public GpsTrackingHub(
        IDeviceTokenService deviceTokenService,
        IGpsTrackingService gpsTrackingService,
        ILogger<GpsTrackingHub> logger)
    {
        _deviceTokenService = deviceTokenService;
        _gpsTrackingService = gpsTrackingService;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        lock (ConnectionLock)
        {
            if (AuthenticatedConnections.TryGetValue(connectionId, out var auth))
            {
                AuthenticatedConnections.Remove(connectionId);
                _logger.LogInformation("Device disconnected: ConnectionId={ConnectionId}, BusId={BusId}",
                    connectionId, auth.BusId);
            }
        }

        await _deviceTokenService.ClearConnectionAsync(connectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task AuthenticateDevice(string token)
    {
        var connectionId = Context.ConnectionId;

        var (isValid, busId, busNumber, error) = await _deviceTokenService.ValidateTokenAsync(token);

        if (!isValid || !busId.HasValue)
        {
            _logger.LogWarning("Authentication failed for ConnectionId={ConnectionId}: {Error}", connectionId, error);
            await Clients.Caller.SendAsync("AuthenticationFailed", new HubErrorDto
            {
                Code = "AUTH_FAILED",
                Message = error ?? "Invalid or expired device token"
            });
            return;
        }

        await _deviceTokenService.UpdateConnectionAsync(token, connectionId);

        lock (ConnectionLock)
        {
            AuthenticatedConnections[connectionId] = (busId.Value, token);
        }

        await Groups.AddToGroupAsync(connectionId, $"bus-{busId.Value}");

        _logger.LogInformation("Device authenticated: ConnectionId={ConnectionId}, BusId={BusId}, BusNumber={BusNumber}",
            connectionId, busId.Value, busNumber);

        await Clients.Caller.SendAsync("AuthenticationSucceeded", new AuthenticationResultDto
        {
            Success = true,
            BusId = busId.Value,
            BusNumber = busNumber
        });
    }

    public async Task SendGpsUpdate(double latitude, double longitude, double? speed = null, double? heading = null)
    {
        var connectionId = Context.ConnectionId;

        int busId;
        bool isAuthenticated;
        lock (ConnectionLock)
        {
            isAuthenticated = AuthenticatedConnections.TryGetValue(connectionId, out var auth);
            busId = isAuthenticated ? auth.BusId : 0;
        }

        if (!isAuthenticated)
        {
            _logger.LogWarning("Unauthenticated GPS update attempt: ConnectionId={ConnectionId}", connectionId);
            await Clients.Caller.SendAsync("Error", new HubErrorDto
            {
                Code = "NOT_AUTHENTICATED",
                Message = "Device must authenticate before sending GPS updates"
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

        var positionUpdate = await _gpsTrackingService.ProcessGpsUpdateAsync(busId, update);

        if (positionUpdate == null)
        {
            await Clients.Caller.SendAsync("Error", new HubErrorDto
            {
                Code = "BUS_NOT_FOUND",
                Message = "Associated bus not found"
            });
            return;
        }

        await Clients.Group($"subscribers-bus-{busId}").SendAsync("BusPositionUpdated", positionUpdate);
        await Clients.Group("subscribers-all").SendAsync("BusPositionUpdated", positionUpdate);

        _logger.LogDebug("GPS update processed: BusId={BusId}, Lat={Lat}, Lng={Lng}",
            busId, latitude, longitude);
    }

    public async Task SubscribeToBus(int busId)
    {
        var connectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(connectionId, $"subscribers-bus-{busId}");
        _logger.LogInformation("Client subscribed to bus: ConnectionId={ConnectionId}, BusId={BusId}",
            connectionId, busId);
    }

    public async Task UnsubscribeFromBus(int busId)
    {
        var connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, $"subscribers-bus-{busId}");
        _logger.LogInformation("Client unsubscribed from bus: ConnectionId={ConnectionId}, BusId={BusId}",
            connectionId, busId);
    }

    public async Task SubscribeToAllBuses()
    {
        var connectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(connectionId, "subscribers-all");
        _logger.LogInformation("Client subscribed to all buses: ConnectionId={ConnectionId}", connectionId);
    }

    public async Task UnsubscribeFromAllBuses()
    {
        var connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, "subscribers-all");
        _logger.LogInformation("Client unsubscribed from all buses: ConnectionId={ConnectionId}", connectionId);
    }
}
