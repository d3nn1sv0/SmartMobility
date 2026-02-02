namespace SmartMobilityApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("PassengerPage", typeof(PassengerPage));
        Routing.RegisterRoute("DriverPage", typeof(DriverPage));
        Routing.RegisterRoute("BusMapPage", typeof(BusMapPage));
    }
}
