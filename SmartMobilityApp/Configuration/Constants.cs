namespace SmartMobilityApp.Configuration;

public static class Constants
{
    public static class Api
    {
        public const string BaseUrl = "http://10.0.2.2:5174/api/";
        public const string HubUrl = "http://10.0.2.2:5174/hubs/gpstracking";
    }

    public static class Geolocation
    {
        public const int MinimumTimeMs = 500;
        public const int TimeoutSeconds = 10;
    }
}
