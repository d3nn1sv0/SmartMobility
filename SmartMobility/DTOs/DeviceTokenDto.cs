namespace SmartMobility.DTOs;

public class DeviceTokenDto
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public int BusId { get; set; }
    public string? BusNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsConnected { get; set; }
}

public class DeviceTokenDetailDto : DeviceTokenDto
{
    public string? CurrentConnectionId { get; set; }
}

public class CreateDeviceTokenDto
{
    public string DeviceName { get; set; } = string.Empty;
    public int BusId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CreateDeviceTokenResponseDto
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public int BusId { get; set; }
    public string? BusNumber { get; set; }
    public string PlainToken { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateDeviceTokenDto
{
    public string DeviceName { get; set; } = string.Empty;
    public int BusId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
