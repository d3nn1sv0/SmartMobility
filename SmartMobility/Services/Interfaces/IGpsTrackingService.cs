using SmartMobility.DTOs;

namespace SmartMobility.Services.Interfaces;

public interface IGpsTrackingService
{
    Task<BusPositionUpdateDto?> ProcessGpsUpdateAsync(int busId, GpsUpdateDto update);
}
