namespace SmartMobility.DTOs;

public class BusDto
{
    public int Id { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public bool IsActive { get; set; }
    public int? CurrentRouteId { get; set; }
    public string? CurrentRouteName { get; set; }
}

public class BusDetailDto
{
    public int Id { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public bool IsActive { get; set; }
    public int? CurrentRouteId { get; set; }
    public string? CurrentRouteName { get; set; }
    public BusPositionDto? LastPosition { get; set; }
}

public class CreateBusDto
{
    public string BusNumber { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
}

public class UpdateBusDto
{
    public string BusNumber { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public bool IsActive { get; set; }
    public int? CurrentRouteId { get; set; }
}
