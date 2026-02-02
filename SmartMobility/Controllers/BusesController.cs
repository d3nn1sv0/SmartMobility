using SmartMobility.Services;

namespace SmartMobility.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusesController : ControllerBase
{
    private readonly SmartMobilityDbContext _context;

    public BusesController(SmartMobilityDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusDto>>> GetBuses()
    {
        var buses = await _context.Buses
            .Include(b => b.CurrentRoute)
            .Select(b => new BusDto
            {
                Id = b.Id,
                BusNumber = b.BusNumber,
                LicensePlate = b.LicensePlate,
                IsActive = b.IsActive,
                CurrentRouteId = b.CurrentRouteId,
                CurrentRouteName = b.CurrentRoute != null ? b.CurrentRoute.Name : null
            })
            .ToListAsync();

        return Ok(buses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BusDetailDto>> GetBus(int id)
    {
        var bus = await _context.Buses
            .Include(b => b.CurrentRoute)
            .Include(b => b.Positions.OrderByDescending(p => p.Timestamp).Take(1))
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bus == null)
        {
            return NotFound();
        }

        var lastPosition = bus.Positions.FirstOrDefault();
        BusPositionDto? positionDto = null;

        if (lastPosition != null)
        {
            positionDto = new BusPositionDto
            {
                Id = lastPosition.Id,
                BusId = lastPosition.BusId,
                Latitude = lastPosition.Latitude,
                Longitude = lastPosition.Longitude,
                Speed = lastPosition.Speed,
                Heading = lastPosition.Heading,
                Timestamp = lastPosition.Timestamp
            };
        }

        return Ok(new BusDetailDto
        {
            Id = bus.Id,
            BusNumber = bus.BusNumber,
            LicensePlate = bus.LicensePlate,
            IsActive = bus.IsActive,
            CurrentRouteId = bus.CurrentRouteId,
            CurrentRouteName = bus.CurrentRoute?.Name,
            LastPosition = positionDto
        });
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<BusLocationDto>>> GetActiveBuses()
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

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<BusDto>> CreateBus(CreateBusDto dto)
    {
        var bus = new Bus
        {
            BusNumber = dto.BusNumber,
            LicensePlate = dto.LicensePlate,
            IsActive = true
        };

        _context.Buses.Add(bus);
        await _context.SaveChangesAsync();

        var result = new BusDto
        {
            Id = bus.Id,
            BusNumber = bus.BusNumber,
            LicensePlate = bus.LicensePlate,
            IsActive = bus.IsActive,
            CurrentRouteId = null,
            CurrentRouteName = null
        };

        return CreatedAtAction(nameof(GetBus), new { id = bus.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBus(int id, UpdateBusDto dto)
    {
        var bus = await _context.Buses.FindAsync(id);

        if (bus == null)
        {
            return NotFound();
        }

        bus.BusNumber = dto.BusNumber;
        bus.LicensePlate = dto.LicensePlate;
        bus.IsActive = dto.IsActive;
        bus.CurrentRouteId = dto.CurrentRouteId;

        await _context.SaveChangesAsync();

        GpsTrackingService.InvalidateBusCache(id);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBus(int id)
    {
        var bus = await _context.Buses.FindAsync(id);

        if (bus == null)
        {
            return NotFound();
        }

        _context.Buses.Remove(bus);
        await _context.SaveChangesAsync();

        GpsTrackingService.InvalidateBusCache(id);

        return NoContent();
    }
}
