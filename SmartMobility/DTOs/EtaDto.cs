namespace SmartMobility.DTOs;

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
