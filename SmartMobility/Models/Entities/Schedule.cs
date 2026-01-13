namespace SmartMobility.Models.Entities;

public class Schedule
{
    public int Id { get; set; }

    public int RouteId { get; set; }

    public Route Route { get; set; } = null!;

    public int? BusId { get; set; }

    public Bus? Bus { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly DepartureTime { get; set; }

    public TimeOnly? ArrivalTime { get; set; }

    public bool IsActive { get; set; } = true;
}
