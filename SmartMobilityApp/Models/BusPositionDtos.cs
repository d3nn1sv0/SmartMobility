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
