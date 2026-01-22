namespace SmartMobility.DTOs;

public class NextStopNotificationDto
{
    public int BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public int StopId { get; set; }
    public string StopName { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
    public double DistanceMeters { get; set; }
}
