using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMobility.Data;
using SmartMobility.DTOs;
using SmartMobility.Models.Entities;
using SmartMobility.Services;

namespace SmartMobility.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly SmartMobilityDbContext _context;

    public RoutesController(SmartMobilityDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RouteDto>>> GetRoutes()
    {
        var routes = await _context.Routes
            .Select(r => new RouteDto
            {
                Id = r.Id,
                RouteNumber = r.RouteNumber,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(routes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RouteDetailDto>> GetRoute(int id)
    {
        var route = await _context.Routes
            .Include(r => r.RouteStops)
                .ThenInclude(rs => rs.Stop)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (route == null)
        {
            return NotFound();
        }

        var stops = route.RouteStops
            .OrderBy(rs => rs.StopOrder)
            .Select(rs => new RouteStopDto
            {
                StopId = rs.StopId,
                StopName = rs.Stop.Name,
                StopOrder = rs.StopOrder,
                EstimatedMinutesFromStart = rs.EstimatedMinutesFromStart
            })
            .ToList();

        return Ok(new RouteDetailDto
        {
            Id = route.Id,
            RouteNumber = route.RouteNumber,
            Name = route.Name,
            Description = route.Description,
            IsActive = route.IsActive,
            Stops = stops
        });
    }

    [HttpPost]
    public async Task<ActionResult<RouteDto>> CreateRoute(CreateRouteDto dto)
    {
        var route = new Models.Entities.Route
        {
            RouteNumber = dto.RouteNumber,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true
        };

        _context.Routes.Add(route);
        await _context.SaveChangesAsync();

        var result = new RouteDto
        {
            Id = route.Id,
            RouteNumber = route.RouteNumber,
            Name = route.Name,
            Description = route.Description,
            IsActive = route.IsActive
        };

        return CreatedAtAction(nameof(GetRoute), new { id = route.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoute(int id, UpdateRouteDto dto)
    {
        var route = await _context.Routes.FindAsync(id);

        if (route == null)
        {
            return NotFound();
        }

        route.RouteNumber = dto.RouteNumber;
        route.Name = dto.Name;
        route.Description = dto.Description;
        route.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        GpsTrackingService.InvalidateAllBusCache();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        var route = await _context.Routes.FindAsync(id);

        if (route == null)
        {
            return NotFound();
        }

        _context.Routes.Remove(route);
        await _context.SaveChangesAsync();

        GpsTrackingService.InvalidateAllBusCache();

        return NoContent();
    }

    [HttpPost("{id}/stops")]
    public async Task<IActionResult> AddStopToRoute(int id, AddStopToRouteDto dto)
    {
        var route = await _context.Routes.FindAsync(id);
        if (route == null)
        {
            return NotFound("Route not found");
        }

        var stop = await _context.Stops.FindAsync(dto.StopId);
        if (stop == null)
        {
            return NotFound("Stop not found");
        }

        var existingOrder = await _context.RouteStops
            .AnyAsync(rs => rs.RouteId == id && rs.StopOrder == dto.StopOrder);

        if (existingOrder)
        {
            return BadRequest("A stop with this order already exists on this route");
        }

        var routeStop = new RouteStop
        {
            RouteId = id,
            StopId = dto.StopId,
            StopOrder = dto.StopOrder,
            EstimatedMinutesFromStart = dto.EstimatedMinutesFromStart
        };

        _context.RouteStops.Add(routeStop);
        await _context.SaveChangesAsync();

        GpsTrackingService.InvalidateAllBusCache();

        return Ok();
    }

    [HttpDelete("{id}/stops/{stopId}")]
    public async Task<IActionResult> RemoveStopFromRoute(int id, int stopId)
    {
        var routeStop = await _context.RouteStops
            .FirstOrDefaultAsync(rs => rs.RouteId == id && rs.StopId == stopId);

        if (routeStop == null)
        {
            return NotFound();
        }

        _context.RouteStops.Remove(routeStop);
        await _context.SaveChangesAsync();

        GpsTrackingService.InvalidateAllBusCache();

        return NoContent();
    }
}
