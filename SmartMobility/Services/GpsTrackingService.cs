using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SmartMobility.Data;
using SmartMobility.DTOs;
using SmartMobility.Models.Entities;
using SmartMobility.Services.Interfaces;

namespace SmartMobility.Services;

public class GpsTrackingService : IGpsTrackingService
{
    private readonly SmartMobilityDbContext _context;
    private readonly IEtaService _etaService;
    private const double NotificationDistanceMeters = 100.0;
    private static readonly ConcurrentDictionary<string, DateTime> NotifiedStops = new();
    private static readonly TimeSpan NotificationCooldown = TimeSpan.FromMinutes(5);

    public GpsTrackingService(SmartMobilityDbContext context, IEtaService etaService)
    {
        _context = context;
        _etaService = etaService;
    }

    public async Task<GpsProcessingResult?> ProcessGpsUpdateAsync(int busId, GpsUpdateDto update)
    {
        var bus = await _context.Buses
            .Include(b => b.CurrentRoute)
                .ThenInclude(r => r!.RouteStops)
                    .ThenInclude(rs => rs.Stop)
            .FirstOrDefaultAsync(b => b.Id == busId);

        if (bus == null)
            return null;

        var position = new BusPosition
        {
            BusId = busId,
            Latitude = update.Latitude,
            Longitude = update.Longitude,
            Speed = update.Speed,
            Heading = update.Heading,
            Timestamp = DateTime.UtcNow
        };

        _context.BusPositions.Add(position);
        await _context.SaveChangesAsync();

        var positionUpdate = new BusPositionUpdateDto
        {
            BusId = bus.Id,
            BusNumber = bus.BusNumber,
            RouteId = bus.CurrentRouteId,
            RouteName = bus.CurrentRoute?.Name,
            Latitude = position.Latitude,
            Longitude = position.Longitude,
            Speed = position.Speed,
            Heading = position.Heading,
            Timestamp = position.Timestamp
        };

        var result = new GpsProcessingResult
        {
            PositionUpdate = positionUpdate
        };

        if (bus.CurrentRoute?.RouteStops != null)
        {
            var notification = CheckForApproachingStop(bus, update.Latitude, update.Longitude);
            if (notification != null)
            {
                result.NextStopNotification = notification;
            }
        }

        return result;
    }

    private NextStopNotificationDto? CheckForApproachingStop(Bus bus, double busLat, double busLon)
    {
        if (bus.CurrentRoute?.RouteStops == null || !bus.CurrentRoute.RouteStops.Any())
            return null;

        var routeStops = bus.CurrentRoute.RouteStops.OrderBy(rs => rs.StopOrder).ToList();

        foreach (var routeStop in routeStops)
        {
            var distance = _etaService.CalculateDistanceMeters(
                busLat, busLon,
                routeStop.Stop.Latitude, routeStop.Stop.Longitude);

            if (distance <= NotificationDistanceMeters)
            {
                var notificationKey = $"{bus.Id}-{routeStop.StopId}";

                if (NotifiedStops.TryGetValue(notificationKey, out var lastNotified))
                {
                    if (DateTime.UtcNow - lastNotified < NotificationCooldown)
                    {
                        continue;
                    }
                }

                NotifiedStops[notificationKey] = DateTime.UtcNow;

                CleanupOldNotifications();

                var estimatedSeconds = (int)Math.Ceiling((distance / 1000.0) / 30.0 * 3600);

                return new NextStopNotificationDto
                {
                    BusId = bus.Id,
                    BusNumber = bus.BusNumber,
                    StopId = routeStop.StopId,
                    StopName = routeStop.Stop.Name,
                    EstimatedSeconds = estimatedSeconds,
                    DistanceMeters = distance
                };
            }
        }

        return null;
    }

    private static void CleanupOldNotifications()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(30);
        var keysToRemove = NotifiedStops
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            NotifiedStops.TryRemove(key, out _);
        }
    }
}
