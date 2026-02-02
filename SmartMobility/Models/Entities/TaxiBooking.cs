namespace SmartMobility.Models.Entities;

public class TaxiBooking
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int? TaxiId { get; set; }

    public Taxi? Taxi { get; set; }

    public double PickupLatitude { get; set; }

    public double PickupLongitude { get; set; }

    [MaxLength(200)]
    public string? PickupAddress { get; set; }

    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }

    [MaxLength(200)]
    public string? DestinationAddress { get; set; }

    public DateTime RequestedPickupTime { get; set; }

    public DateTime? EstimatedArrivalTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
