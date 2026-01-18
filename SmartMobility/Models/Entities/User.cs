using System.ComponentModel.DataAnnotations;
using SmartMobility.Models.Enums;

namespace SmartMobility.Models.Entities;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Name { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaxiBooking> TaxiBookings { get; set; } = new List<TaxiBooking>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
