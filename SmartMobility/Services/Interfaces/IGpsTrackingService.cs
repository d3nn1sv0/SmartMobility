namespace SmartMobility.Services.Interfaces;

public class GpsProcessingResult
{
    public BusPositionUpdateDto? PositionUpdate { get; set; }
    public NextStopNotificationDto? NextStopNotification { get; set; }
}

public interface IGpsTrackingService
{
    Task<GpsProcessingResult?> CreatePositionUpdateAsync(int busId, GpsUpdateDto update);

    void SavePositionInBackground(int busId, GpsUpdateDto update);
}
