using Microsoft.EntityFrameworkCore;
using SmartMobility.Models.Entities;
using Route = SmartMobility.Models.Entities.Route;

namespace SmartMobility.Data;

public class SmartMobilityDbContext : DbContext
{
    public SmartMobilityDbContext(DbContextOptions<SmartMobilityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<BusPosition> BusPositions => Set<BusPosition>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Taxi> Taxis => Set<Taxi>();
    public DbSet<TaxiBooking> TaxiBookings => Set<TaxiBooking>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Bus -> Route relationship
        modelBuilder.Entity<Bus>()
            .HasOne(b => b.CurrentRoute)
            .WithMany(r => r.Buses)
            .HasForeignKey(b => b.CurrentRouteId)
            .OnDelete(DeleteBehavior.SetNull);

        // RouteStop - many-to-many between Route and Stop
        modelBuilder.Entity<RouteStop>()
            .HasOne(rs => rs.Route)
            .WithMany(r => r.RouteStops)
            .HasForeignKey(rs => rs.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RouteStop>()
            .HasOne(rs => rs.Stop)
            .WithMany(s => s.RouteStops)
            .HasForeignKey(rs => rs.StopId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RouteStop>()
            .HasIndex(rs => new { rs.RouteId, rs.StopOrder })
            .IsUnique();

        // Schedule
        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Route)
            .WithMany(r => r.Schedules)
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Bus)
            .WithMany(b => b.Schedules)
            .HasForeignKey(s => s.BusId)
            .OnDelete(DeleteBehavior.SetNull);

        // BusPosition
        modelBuilder.Entity<BusPosition>()
            .HasOne(bp => bp.Bus)
            .WithMany(b => b.Positions)
            .HasForeignKey(bp => bp.BusId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BusPosition>()
            .HasIndex(bp => bp.Timestamp);

        // Notification
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaxiBooking
        modelBuilder.Entity<TaxiBooking>()
            .HasOne(tb => tb.User)
            .WithMany(u => u.TaxiBookings)
            .HasForeignKey(tb => tb.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaxiBooking>()
            .HasOne(tb => tb.Taxi)
            .WithMany(t => t.Bookings)
            .HasForeignKey(tb => tb.TaxiId)
            .OnDelete(DeleteBehavior.SetNull);

        // DeviceToken
        modelBuilder.Entity<DeviceToken>()
            .HasOne(dt => dt.Bus)
            .WithMany(b => b.DeviceTokens)
            .HasForeignKey(dt => dt.BusId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeviceToken>()
            .HasIndex(dt => dt.Token)
            .IsUnique();
    }
}
