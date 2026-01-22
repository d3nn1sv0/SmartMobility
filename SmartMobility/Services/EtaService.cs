using Microsoft.EntityFrameworkCore;
using SmartMobility.Data;
using SmartMobility.DTOs;
using SmartMobility.Services.Interfaces;

namespace SmartMobility.Services;

public class EtaService : IEtaService
{
    private readonly SmartMobilityDbContext _context;
    private const double AverageSpeedKmh = 30.0;
    private const double EarthRadiusKm = 6371.0;

    public EtaService(SmartMobilityDbContext context)
    {
        _context = context;
    }

    public async Task<BusEtaResponseDto?> GetEtaForBusAsync(int busId)
    {
        var bus = await _context.Buses
            .Include(b => b.CurrentRoute)
                .ThenInclude(r => r!.RouteStops)
                    .ThenInclude(rs => rs.Stop)
            .Include(b => b.Positions.OrderByDescending(p => p.Timestamp).Take(1))
            .FirstOrDefaultAsync(b => b.Id == busId);

        if (bus == null)
            return null;

        var latestPosition = bus.Positions.FirstOrDefault();
        if (latestPosition == null || bus.CurrentRoute == null)
        {
            return new BusEtaResponseDto
            {
                BusId = bus.Id,
                BusNumber = bus.BusNumber,
                RouteId = bus.CurrentRouteId,
                RouteName = bus.CurrentRoute?.Name,
                Stops = new List<StopEtaDto>()
            };
        }

        var routeStops = bus.CurrentRoute.RouteStops
            .OrderBy(rs => rs.StopOrder)
            .ToList();

        var stopEtas = new List<StopEtaDto>();
        int nextStopIndex = FindNextStopIndex(latestPosition.Latitude, latestPosition.Longitude, routeStops);
        double cumulativeDistance = 0;

        for (int i = 0; i < routeStops.Count; i++)
        {
            var routeStop = routeStops[i];
            double distanceFromBus;

            if (i < nextStopIndex)
            {
                distanceFromBus = 0;
            }
            else if (i == nextStopIndex)
            {
                distanceFromBus = CalculateDistanceMeters(
                    latestPosition.Latitude, latestPosition.Longitude,
                    routeStop.Stop.Latitude, routeStop.Stop.Longitude);
                cumulativeDistance = distanceFromBus;
            }
            else
            {
                var prevStop = routeStops[i - 1];
                var segmentDistance = CalculateDistanceMeters(
                    prevStop.Stop.Latitude, prevStop.Stop.Longitude,
                    routeStop.Stop.Latitude, routeStop.Stop.Longitude);
                cumulativeDistance += segmentDistance;
                distanceFromBus = cumulativeDistance;
            }

            var estimatedSeconds = (int)Math.Ceiling((distanceFromBus / 1000.0) / AverageSpeedKmh * 3600);

            stopEtas.Add(new StopEtaDto
            {
                StopId = routeStop.StopId,
                StopName = routeStop.Stop.Name,
                StopOrder = routeStop.StopOrder,
                EstimatedSeconds = i < nextStopIndex ? 0 : estimatedSeconds,
                DistanceMeters = distanceFromBus,
                IsNextStop = i == nextStopIndex
            });
        }

        return new BusEtaResponseDto
        {
            BusId = bus.Id,
            BusNumber = bus.BusNumber,
            RouteId = bus.CurrentRouteId,
            RouteName = bus.CurrentRoute.Name,
            Stops = stopEtas
        };
    }

    public async Task<StopEtaDto?> GetNextStopForBusAsync(int busId)
    {
        var eta = await GetEtaForBusAsync(busId);
        return eta?.Stops.FirstOrDefault(s => s.IsNextStop);
    }

    private int FindNextStopIndex(double busLat, double busLon, List<Models.Entities.RouteStop> routeStops)
    {
        if (routeStops.Count == 0)
            return 0;

        double minDistance = double.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < routeStops.Count; i++)
        {
            var stop = routeStops[i].Stop;
            var distance = CalculateDistanceMeters(busLat, busLon, stop.Latitude, stop.Longitude);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex < routeStops.Count - 1)
        {
            var closestStop = routeStops[closestIndex].Stop;
            var nextStop = routeStops[closestIndex + 1].Stop;

            var distToClosest = CalculateDistanceMeters(busLat, busLon, closestStop.Latitude, closestStop.Longitude);
            var distClosestToNext = CalculateDistanceMeters(closestStop.Latitude, closestStop.Longitude, nextStop.Latitude, nextStop.Longitude);

            if (distToClosest < 50)
            {
                return closestIndex + 1;
            }

            var distToNext = CalculateDistanceMeters(busLat, busLon, nextStop.Latitude, nextStop.Longitude);
            if (distToClosest + distToNext < distClosestToNext * 1.5)
            {
                return closestIndex + 1;
            }
        }

        return closestIndex;
    }

    public double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c * 1000;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
