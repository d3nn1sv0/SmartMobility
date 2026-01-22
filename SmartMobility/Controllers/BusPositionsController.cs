using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMobility.Data;
using SmartMobility.DTOs;
using SmartMobility.Models.Entities;
using SmartMobility.Services.Interfaces;

namespace SmartMobility.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusPositionsController : ControllerBase
{
    private readonly SmartMobilityDbContext _context;
    private readonly IEtaService _etaService;

    public BusPositionsController(SmartMobilityDbContext context, IEtaService etaService)
    {
        _context = context;
        _etaService = etaService;
    }

    [HttpGet("bus/{busId}")]
    public async Task<ActionResult<IEnumerable<BusPositionDto>>> GetPositionsForBus(
        int busId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100)
    {
        var query = _context.BusPositions
            .Where(p => p.BusId == busId);

        if (from.HasValue)
        {
            query = query.Where(p => p.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.Timestamp <= to.Value);
        }

        var positions = await query
            .OrderByDescending(p => p.Timestamp)
            .Take(limit)
            .Select(p => new BusPositionDto
            {
                Id = p.Id,
                BusId = p.BusId,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Speed = p.Speed,
                Heading = p.Heading,
                Timestamp = p.Timestamp
            })
            .ToListAsync();

        return Ok(positions);
    }

    [HttpGet("bus/{busId}/latest")]
    public async Task<ActionResult<BusPositionDto>> GetLatestPosition(int busId)
    {
        var position = await _context.BusPositions
            .Where(p => p.BusId == busId)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync();

        if (position == null)
        {
            return NotFound();
        }

        return Ok(new BusPositionDto
        {
            Id = position.Id,
            BusId = position.BusId,
            Latitude = position.Latitude,
            Longitude = position.Longitude,
            Speed = position.Speed,
            Heading = position.Heading,
            Timestamp = position.Timestamp
        });
    }

    [HttpGet("all/latest")]
    public async Task<ActionResult<IEnumerable<BusLocationDto>>> GetAllLatestPositions()
    {
        var buses = await _context.Buses
            .Where(b => b.IsActive)
            .Include(b => b.CurrentRoute)
            .Include(b => b.Positions.OrderByDescending(p => p.Timestamp).Take(1))
            .ToListAsync();

        var result = buses
            .Where(b => b.Positions.Any())
            .Select(b =>
            {
                var pos = b.Positions.First();
                return new BusLocationDto
                {
                    BusId = b.Id,
                    BusNumber = b.BusNumber,
                    RouteName = b.CurrentRoute?.Name,
                    Latitude = pos.Latitude,
                    Longitude = pos.Longitude,
                    Speed = pos.Speed,
                    Heading = pos.Heading,
                    Timestamp = pos.Timestamp
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BusPositionDto>> ReportPosition(CreateBusPositionDto dto)
    {
        var bus = await _context.Buses.FindAsync(dto.BusId);
        if (bus == null)
        {
            return NotFound("Bus not found");
        }

        var position = new BusPosition
        {
            BusId = dto.BusId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Speed = dto.Speed,
            Heading = dto.Heading,
            Timestamp = DateTime.UtcNow
        };

        _context.BusPositions.Add(position);
        await _context.SaveChangesAsync();

        var result = new BusPositionDto
        {
            Id = position.Id,
            BusId = position.BusId,
            Latitude = position.Latitude,
            Longitude = position.Longitude,
            Speed = position.Speed,
            Heading = position.Heading,
            Timestamp = position.Timestamp
        };

        return CreatedAtAction(nameof(GetLatestPosition), new { busId = position.BusId }, result);
    }

    [HttpGet("bus/{busId}/eta")]
    public async Task<ActionResult<BusEtaResponseDto>> GetEtaForBus(int busId)
    {
        var eta = await _etaService.GetEtaForBusAsync(busId);

        if (eta == null)
        {
            return NotFound("Bus not found");
        }

        return Ok(eta);
    }

    [HttpGet("bus/{busId}/nextstop")]
    public async Task<ActionResult<StopEtaDto>> GetNextStopForBus(int busId)
    {
        var nextStop = await _etaService.GetNextStopForBusAsync(busId);

        if (nextStop == null)
        {
            return NotFound("Next stop not found or bus has no route");
        }

        return Ok(nextStop);
    }
}
