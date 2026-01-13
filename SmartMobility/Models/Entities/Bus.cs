using System.ComponentModel.DataAnnotations;

namespace SmartMobility.Models.Entities;

public class Bus
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string BusNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? LicensePlate { get; set; }

    public bool IsActive { get; set; } = true;

    public int? CurrentRouteId { get; set; }

    public Route? CurrentRoute { get; set; }

    public ICollection<BusPosition> Positions { get; set; } = new List<BusPosition>();

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
