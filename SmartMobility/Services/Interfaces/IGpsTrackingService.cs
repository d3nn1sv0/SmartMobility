using SmartMobility.DTOs;

namespace SmartMobility.Services.Interfaces;

public class GpsProcessingResult
{
    public BusPositionUpdateDto? PositionUpdate { get; set; }
    public NextStopNotificationDto? NextStopNotification { get; set; }
}

public interface IGpsTrackingService
{
    Task<GpsProcessingResult?> ProcessGpsUpdateAsync(int busId, GpsUpdateDto update);
}
