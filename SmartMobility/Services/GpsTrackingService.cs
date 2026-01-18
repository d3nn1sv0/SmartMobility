using Microsoft.EntityFrameworkCore;
using SmartMobility.Data;
using SmartMobility.DTOs;
using SmartMobility.Models.Entities;
using SmartMobility.Services.Interfaces;

namespace SmartMobility.Services;

public class GpsTrackingService : IGpsTrackingService
{
    private readonly SmartMobilityDbContext _context;

    public GpsTrackingService(SmartMobilityDbContext context)
    {
        _context = context;
    }

    public async Task<BusPositionUpdateDto?> ProcessGpsUpdateAsync(int busId, GpsUpdateDto update)
    {
        var bus = await _context.Buses
            .Include(b => b.CurrentRoute)
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

        return new BusPositionUpdateDto
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
    }
}
