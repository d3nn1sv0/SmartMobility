using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartMobility.Data;
using SmartMobility.DTOs;
using SmartMobility.Models.Entities;
using SmartMobility.Services.Interfaces;

namespace SmartMobility.Services;

public class DeviceTokenService : IDeviceTokenService
{
    private readonly SmartMobilityDbContext _context;

    public DeviceTokenService(SmartMobilityDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DeviceTokenDto>> GetAllAsync()
    {
        return await _context.DeviceTokens
            .Include(dt => dt.Bus)
            .Select(dt => new DeviceTokenDto
            {
                Id = dt.Id,
                DeviceName = dt.DeviceName,
                BusId = dt.BusId,
                BusNumber = dt.Bus.BusNumber,
                IsActive = dt.IsActive,
                CreatedAt = dt.CreatedAt,
                LastUsedAt = dt.LastUsedAt,
                ExpiresAt = dt.ExpiresAt,
                IsConnected = dt.CurrentConnectionId != null
            })
            .ToListAsync();
    }

    public async Task<DeviceTokenDetailDto?> GetByIdAsync(int id)
    {
        var token = await _context.DeviceTokens
            .Include(dt => dt.Bus)
            .FirstOrDefaultAsync(dt => dt.Id == id);

        if (token == null)
            return null;

        return new DeviceTokenDetailDto
        {
            Id = token.Id,
            DeviceName = token.DeviceName,
            BusId = token.BusId,
            BusNumber = token.Bus.BusNumber,
            IsActive = token.IsActive,
            CreatedAt = token.CreatedAt,
            LastUsedAt = token.LastUsedAt,
            ExpiresAt = token.ExpiresAt,
            IsConnected = token.CurrentConnectionId != null,
            CurrentConnectionId = token.CurrentConnectionId
        };
    }

    public async Task<IEnumerable<DeviceTokenDto>> GetByBusIdAsync(int busId)
    {
        return await _context.DeviceTokens
            .Include(dt => dt.Bus)
            .Where(dt => dt.BusId == busId)
            .Select(dt => new DeviceTokenDto
            {
                Id = dt.Id,
                DeviceName = dt.DeviceName,
                BusId = dt.BusId,
                BusNumber = dt.Bus.BusNumber,
                IsActive = dt.IsActive,
                CreatedAt = dt.CreatedAt,
                LastUsedAt = dt.LastUsedAt,
                ExpiresAt = dt.ExpiresAt,
                IsConnected = dt.CurrentConnectionId != null
            })
            .ToListAsync();
    }

    public async Task<CreateDeviceTokenResponseDto> CreateAsync(CreateDeviceTokenDto dto)
    {
        var bus = await _context.Buses.FindAsync(dto.BusId);
        if (bus == null)
            throw new ArgumentException($"Bus with id {dto.BusId} not found");

        var plainToken = GeneratePlainToken();
        var hashedToken = HashToken(plainToken);

        var deviceToken = new DeviceToken
        {
            Token = hashedToken,
            DeviceName = dto.DeviceName,
            BusId = dto.BusId,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.DeviceTokens.Add(deviceToken);
        await _context.SaveChangesAsync();

        return new CreateDeviceTokenResponseDto
        {
            Id = deviceToken.Id,
            DeviceName = deviceToken.DeviceName,
            BusId = deviceToken.BusId,
            BusNumber = bus.BusNumber,
            PlainToken = plainToken,
            ExpiresAt = deviceToken.ExpiresAt
        };
    }

    public async Task<DeviceTokenDto?> UpdateAsync(int id, UpdateDeviceTokenDto dto)
    {
        var token = await _context.DeviceTokens
            .Include(dt => dt.Bus)
            .FirstOrDefaultAsync(dt => dt.Id == id);

        if (token == null)
            return null;

        var bus = await _context.Buses.FindAsync(dto.BusId);
        if (bus == null)
            throw new ArgumentException($"Bus with id {dto.BusId} not found");

        token.DeviceName = dto.DeviceName;
        token.BusId = dto.BusId;
        token.IsActive = dto.IsActive;
        token.ExpiresAt = dto.ExpiresAt;

        await _context.SaveChangesAsync();

        return new DeviceTokenDto
        {
            Id = token.Id,
            DeviceName = token.DeviceName,
            BusId = token.BusId,
            BusNumber = bus.BusNumber,
            IsActive = token.IsActive,
            CreatedAt = token.CreatedAt,
            LastUsedAt = token.LastUsedAt,
            ExpiresAt = token.ExpiresAt,
            IsConnected = token.CurrentConnectionId != null
        };
    }

    public async Task<bool> RevokeAsync(int id)
    {
        var token = await _context.DeviceTokens.FindAsync(id);
        if (token == null)
            return false;

        token.IsActive = false;
        token.CurrentConnectionId = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var token = await _context.DeviceTokens.FindAsync(id);
        if (token == null)
            return false;

        _context.DeviceTokens.Remove(token);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool IsValid, int? BusId, string? BusNumber, string? Error)> ValidateTokenAsync(string plainToken)
    {
        if (string.IsNullOrWhiteSpace(plainToken))
            return (false, null, null, "Token is empty");

        // Trim whitespace and remove any quotes
        plainToken = plainToken.Trim().Trim('"');

        string hashedToken;
        try
        {
            hashedToken = HashToken(plainToken);
        }
        catch (FormatException)
        {
            return (false, null, null, "Invalid token format (must be hex string)");
        }

        var token = await _context.DeviceTokens
            .Include(dt => dt.Bus)
            .FirstOrDefaultAsync(dt => dt.Token == hashedToken);

        if (token == null)
            return (false, null, null, "Token not found");

        if (!token.IsActive)
            return (false, null, null, "Token is inactive/revoked");

        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value < DateTime.UtcNow)
            return (false, null, null, $"Token expired at {token.ExpiresAt.Value:u}");

        token.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, token.BusId, token.Bus.BusNumber, null);
    }

    public async Task UpdateConnectionAsync(string plainToken, string? connectionId)
    {
        var hashedToken = HashToken(plainToken);

        var token = await _context.DeviceTokens
            .FirstOrDefaultAsync(dt => dt.Token == hashedToken);

        if (token != null)
        {
            token.CurrentConnectionId = connectionId;
            token.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearConnectionAsync(string connectionId)
    {
        var token = await _context.DeviceTokens
            .FirstOrDefaultAsync(dt => dt.CurrentConnectionId == connectionId);

        if (token != null)
        {
            token.CurrentConnectionId = null;
            await _context.SaveChangesAsync();
        }
    }

    private static string GeneratePlainToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashToken(string plainToken)
    {
        var bytes = Convert.FromHexString(plainToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
