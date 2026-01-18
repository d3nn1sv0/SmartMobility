namespace SmartMobility.DTOs;

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

public class AuthenticationResultDto
{
    public bool Success { get; set; }
    public int? BusId { get; set; }
    public string? BusNumber { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class HubErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
