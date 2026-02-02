namespace SmartMobility.Services.Interfaces;

public interface IEtaService
{
    Task<BusEtaResponseDto?> GetEtaForBusAsync(int busId);
    Task<StopEtaDto?> GetNextStopForBusAsync(int busId);
    double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2);
}
