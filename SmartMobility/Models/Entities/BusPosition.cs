namespace SmartMobility.Models.Entities;

public class BusPosition
{
    public int Id { get; set; }

    public int BusId { get; set; }

    public Bus Bus { get; set; } = null!;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double? Speed { get; set; }

    public double? Heading { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
