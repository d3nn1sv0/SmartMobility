namespace SmartMobilityApp.Services.Interfaces;

public interface IBusAnimationService
{
    event EventHandler? PositionsUpdated;
    IReadOnlyCollection<AnimatedBusPosition> Buses { get; }

    void Start();
    void Stop();
    void UpdateTargetPosition(int busId, string busNumber, string? routeName,
        double latitude, double longitude, double? speed, double? heading, DateTime timestamp);
    void RemoveBus(int busId);
    void Clear();
}
