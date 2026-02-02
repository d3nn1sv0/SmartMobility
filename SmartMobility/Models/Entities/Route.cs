namespace SmartMobility.Models.Entities;

public class Route
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string RouteNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();

    public ICollection<Bus> Buses { get; set; } = new List<Bus>();

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
