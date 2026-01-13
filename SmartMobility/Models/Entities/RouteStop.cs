namespace SmartMobility.Models.Entities;

public class RouteStop
{
    public int Id { get; set; }

    public int RouteId { get; set; }

    public Route Route { get; set; } = null!;

    public int StopId { get; set; }

    public Stop Stop { get; set; } = null!;

    public int StopOrder { get; set; }

    public int? EstimatedMinutesFromStart { get; set; }
}
