using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMobility.Data;
using SmartMobility.DTOs;
using SmartMobility.Models.Entities;

namespace SmartMobility.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StopsController : ControllerBase
{
    private readonly SmartMobilityDbContext _context;

    public StopsController(SmartMobilityDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StopDto>>> GetStops()
    {
        var stops = await _context.Stops
            .Select(s => new StopDto
            {
                Id = s.Id,
                Name = s.Name,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Address = s.Address
            })
            .ToListAsync();

        return Ok(stops);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StopDto>> GetStop(int id)
    {
        var stop = await _context.Stops.FindAsync(id);

        if (stop == null)
        {
            return NotFound();
        }

        return Ok(new StopDto
        {
            Id = stop.Id,
            Name = stop.Name,
            Latitude = stop.Latitude,
            Longitude = stop.Longitude,
            Address = stop.Address
        });
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<IEnumerable<StopDto>>> GetNearbyStops(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKm = 1.0)
    {
        var stops = await _context.Stops.ToListAsync();

        var nearbyStops = stops
            .Select(s => new
            {
                Stop = s,
                Distance = CalculateDistance(latitude, longitude, s.Latitude, s.Longitude)
            })
            .Where(x => x.Distance <= radiusKm)
            .OrderBy(x => x.Distance)
            .Select(x => new StopDto
            {
                Id = x.Stop.Id,
                Name = x.Stop.Name,
                Latitude = x.Stop.Latitude,
                Longitude = x.Stop.Longitude,
                Address = x.Stop.Address
            })
            .ToList();

        return Ok(nearbyStops);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<StopDto>>> SearchStops([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Search term is required");
        }

        var stops = await _context.Stops
            .Where(s => s.Name.ToLower().Contains(name.ToLower()))
            .Select(s => new StopDto
            {
                Id = s.Id,
                Name = s.Name,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Address = s.Address
            })
            .ToListAsync();

        return Ok(stops);
    }

    [HttpPost]
    public async Task<ActionResult<StopDto>> CreateStop(CreateStopDto dto)
    {
        var stop = new Stop
        {
            Name = dto.Name,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address
        };

        _context.Stops.Add(stop);
        await _context.SaveChangesAsync();

        var result = new StopDto
        {
            Id = stop.Id,
            Name = stop.Name,
            Latitude = stop.Latitude,
            Longitude = stop.Longitude,
            Address = stop.Address
        };

        return CreatedAtAction(nameof(GetStop), new { id = stop.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStop(int id, UpdateStopDto dto)
    {
        var stop = await _context.Stops.FindAsync(id);

        if (stop == null)
        {
            return NotFound();
        }

        stop.Name = dto.Name;
        stop.Latitude = dto.Latitude;
        stop.Longitude = dto.Longitude;
        stop.Address = dto.Address;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStop(int id)
    {
        var stop = await _context.Stops.FindAsync(id);

        if (stop == null)
        {
            return NotFound();
        }

        _context.Stops.Remove(stop);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in km
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
