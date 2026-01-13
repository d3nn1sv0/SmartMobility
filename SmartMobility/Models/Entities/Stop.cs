using System.ComponentModel.DataAnnotations;

namespace SmartMobility.Models.Entities;

public class Stop
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
}
