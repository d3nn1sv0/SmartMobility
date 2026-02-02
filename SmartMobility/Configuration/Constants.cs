namespace SmartMobility.Configuration;

public static class Constants
{
    public static class Routes
    {
        public const string ApiBase = "api";
        public const string GpsTrackingHub = "/hubs/gpstracking";
    }

    public static class GpsTracking
    {
        public const double NotificationDistanceMeters = 100.0;
        public const int CacheExpiryMinutes = 5;
        public const int NotificationCooldownSeconds = 30;
        public const int NotificationCleanupMinutes = 30;
    }

    public static class SignalRGroups
    {
        public const string SubscribersAll = "subscribers-all";
        public const string BusPrefix = "bus-";
        public const string SubscribersBusPrefix = "subscribers-bus-";
    }
}
