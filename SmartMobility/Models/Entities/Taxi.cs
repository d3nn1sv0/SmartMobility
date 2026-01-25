using System.ComponentModel.DataAnnotations;
using SmartMobility.Models.Enums;

namespace SmartMobility.Models.Entities;

public class Taxi
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string LicensePlate { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DriverName { get; set; }

    public TaxiStatus Status { get; set; } = TaxiStatus.Offline;

    public double? CurrentLatitude { get; set; }

    public double? CurrentLongitude { get; set; }

    public DateTime? LastPositionUpdate { get; set; }

    public ICollection<TaxiBooking> Bookings { get; set; } = new List<TaxiBooking>();
}
