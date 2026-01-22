namespace SmartMobilityApp.Models;

public class BusPositionDto
{
    public int Id { get; set; }
    public int BusId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CreateBusPositionDto
{
    public int BusId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
}

public class BusLocationDto
{
    public int BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string? RouteName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
}

public class GpsUpdateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
}

public class BusPositionUpdateDto
{
    public int BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string? RouteName { get; set; }
    public int? RouteId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
}

public class NextStopNotificationDto
{
    public int BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public int StopId { get; set; }
    public string StopName { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
    public double DistanceMeters { get; set; }
}

public class StopEtaDto
{
    public int StopId { get; set; }
    public string StopName { get; set; } = string.Empty;
    public int StopOrder { get; set; }
    public int EstimatedSeconds { get; set; }
    public double DistanceMeters { get; set; }
    public bool IsNextStop { get; set; }
}

public class BusEtaResponseDto
{
    public int BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public int? RouteId { get; set; }
    public string? RouteName { get; set; }
    public List<StopEtaDto> Stops { get; set; } = new();
}
