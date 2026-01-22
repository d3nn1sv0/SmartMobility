namespace SmartMobilityApp.Models;

public class RouteDto
{
    public int Id { get; set; }
    public string RouteNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class RouteDetailDto
{
    public int Id { get; set; }
    public string RouteNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<RouteStopDto> Stops { get; set; } = new();
}

public class RouteStopDto
{
    public int StopId { get; set; }
    public string StopName { get; set; } = string.Empty;
    public int StopOrder { get; set; }
    public int? EstimatedMinutesFromStart { get; set; }
}
