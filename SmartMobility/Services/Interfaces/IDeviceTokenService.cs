using SmartMobility.DTOs;

namespace SmartMobility.Services.Interfaces;

public interface IDeviceTokenService
{
    Task<IEnumerable<DeviceTokenDto>> GetAllAsync();
    Task<DeviceTokenDetailDto?> GetByIdAsync(int id);
    Task<IEnumerable<DeviceTokenDto>> GetByBusIdAsync(int busId);
    Task<CreateDeviceTokenResponseDto> CreateAsync(CreateDeviceTokenDto dto);
    Task<DeviceTokenDto?> UpdateAsync(int id, UpdateDeviceTokenDto dto);
    Task<bool> RevokeAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<(bool IsValid, int? BusId, string? BusNumber, string? Error)> ValidateTokenAsync(string plainToken);
    Task UpdateConnectionAsync(string plainToken, string? connectionId);
    Task ClearConnectionAsync(string connectionId);
}
