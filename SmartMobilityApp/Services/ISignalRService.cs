using SmartMobilityApp.Models;

namespace SmartMobilityApp.Services;

public interface ISignalRService
{
    event EventHandler<NextStopNotificationDto>? NextStopApproaching;
    event EventHandler<BusPositionUpdateDto>? BusPositionUpdated;
    event EventHandler<string>? ConnectionStateChanged;

    bool IsConnected { get; }

    Task ConnectAsync();
    Task DisconnectAsync();
    Task SubscribeToBusAsync(int busId);
    Task UnsubscribeFromBusAsync(int busId);
    Task SubscribeToAllBusesAsync();
    Task UnsubscribeFromAllBusesAsync();
}
