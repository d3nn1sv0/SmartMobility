using System.ComponentModel.DataAnnotations;

namespace SmartMobility.Models.Entities;

public class DeviceToken
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)] // SHA-256 hash
    public string Token { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DeviceName { get; set; } = string.Empty;

    public int BusId { get; set; }

    public Bus Bus { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [MaxLength(100)]
    public string? CurrentConnectionId { get; set; }
}
