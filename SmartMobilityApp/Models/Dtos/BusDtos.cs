namespace SmartMobilityApp.Models.Dtos;

public class BusDto
{
    public int Id { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public bool IsActive { get; set; }

    public override string ToString() => $"{BusNumber} ({LicensePlate ?? "Ingen nummerplade"})";
}
