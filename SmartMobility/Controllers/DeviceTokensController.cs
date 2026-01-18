using Microsoft.AspNetCore.Mvc;
using SmartMobility.DTOs;
using SmartMobility.Services.Interfaces;

namespace SmartMobility.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeviceTokensController : ControllerBase
{
    private readonly IDeviceTokenService _deviceTokenService;

    public DeviceTokensController(IDeviceTokenService deviceTokenService)
    {
        _deviceTokenService = deviceTokenService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceTokenDto>>> GetDeviceTokens()
    {
        var tokens = await _deviceTokenService.GetAllAsync();
        return Ok(tokens);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeviceTokenDetailDto>> GetDeviceToken(int id)
    {
        var token = await _deviceTokenService.GetByIdAsync(id);

        if (token == null)
        {
            return NotFound();
        }

        return Ok(token);
    }

    [HttpGet("bus/{busId}")]
    public async Task<ActionResult<IEnumerable<DeviceTokenDto>>> GetDeviceTokensByBus(int busId)
    {
        var tokens = await _deviceTokenService.GetByBusIdAsync(busId);
        return Ok(tokens);
    }

    [HttpPost]
    public async Task<ActionResult<CreateDeviceTokenResponseDto>> CreateDeviceToken(CreateDeviceTokenDto dto)
    {
        try
        {
            var result = await _deviceTokenService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetDeviceToken), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDeviceToken(int id, UpdateDeviceTokenDto dto)
    {
        try
        {
            var result = await _deviceTokenService.UpdateAsync(id, dto);

            if (result == null)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/revoke")]
    public async Task<IActionResult> RevokeDeviceToken(int id)
    {
        var success = await _deviceTokenService.RevokeAsync(id);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDeviceToken(int id)
    {
        var success = await _deviceTokenService.DeleteAsync(id);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
