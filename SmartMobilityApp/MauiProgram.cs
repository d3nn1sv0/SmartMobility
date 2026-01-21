using Microsoft.Extensions.Logging;
using SmartMobilityApp.Services;
using SmartMobilityApp.ViewModels;
using SmartMobilityApp.Views;

namespace SmartMobilityApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<PassengerViewModel>();
        builder.Services.AddTransient<DriverViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<PassengerPage>();
        builder.Services.AddTransient<DriverPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
