using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

const string BaseUrl = "https://localhost:7095";
const string HubUrl = $"{BaseUrl}/hubs/gpstracking";

Console.WriteLine("=== GPS Tracking Test Client ===\n");
Console.WriteLine("1. Bus Simulator (send GPS)");
Console.WriteLine("2. Subscriber (receive GPS updates)");
Console.Write("\nVælg mode (1/2): ");

var mode = Console.ReadLine();

if (mode == "1")
{
    await RunBusSimulator();
}
else if (mode == "2")
{
    await RunSubscriber();
}
else
{
    Console.WriteLine("Ugyldigt valg");
}

async Task RunBusSimulator()
{
    Console.Write("\nIndtast device token: ");
    var token = Console.ReadLine();

    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("Token er påkrævet!");
        return;
    }

    var connection = new HubConnectionBuilder()
        .WithUrl(HubUrl, options =>
        {
            options.HttpMessageHandlerFactory = _ => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
        })
        .WithAutomaticReconnect()
        .Build();

    connection.On<JsonElement>("AuthenticationSucceeded", result =>
    {
        Console.WriteLine($"\n[OK] Authenticated! BusId: {result.GetProperty("busId")}, BusNumber: {result.GetProperty("busNumber")}");
        Console.WriteLine("Tryk ENTER for at sende GPS updates (eller 'q' for at stoppe)\n");
    });

    connection.On<JsonElement>("AuthenticationFailed", error =>
    {
        Console.WriteLine($"\n[FEJL] Authentication failed: {error.GetProperty("message")}");
    });

    connection.On<JsonElement>("Error", error =>
    {
        Console.WriteLine($"\n[FEJL] {error.GetProperty("code")}: {error.GetProperty("message")}");
    });

    try
    {
        Console.WriteLine("Connecting to hub...");
        await connection.StartAsync();
        Console.WriteLine("Connected! Authenticating...");

        await connection.InvokeAsync("AuthenticateDevice", token);

        var random = new Random();
        double lat = 55.6761;
        double lng = 12.5683;

        while (true)
        {
            var input = Console.ReadLine();
            if (input?.ToLower() == "q") break;

            lat += (random.NextDouble() - 0.5) * 0.001;
            lng += (random.NextDouble() - 0.5) * 0.001;
            var speed = random.NextDouble() * 50;
            var heading = random.NextDouble() * 360;

            await connection.InvokeAsync("SendGpsUpdate", lat, lng, speed, heading);
            Console.WriteLine($"[SENT] Lat: {lat:F6}, Lng: {lng:F6}, Speed: {speed:F1} km/h");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    finally
    {
        await connection.StopAsync();
    }
}

async Task RunSubscriber()
{
    Console.Write("\nSubscribe til specifik busId (eller tryk ENTER for alle): ");
    var busIdInput = Console.ReadLine();

    var connection = new HubConnectionBuilder()
        .WithUrl(HubUrl, options =>
        {
            options.HttpMessageHandlerFactory = _ => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
        })
        .WithAutomaticReconnect()
        .Build();

    connection.On<JsonElement>("BusPositionUpdated", position =>
    {
        Console.WriteLine($"[GPS] Bus {position.GetProperty("busNumber")} (ID: {position.GetProperty("busId")}): " +
                          $"Lat: {position.GetProperty("latitude"):F6}, " +
                          $"Lng: {position.GetProperty("longitude"):F6}, " +
                          $"Speed: {position.GetProperty("speed")}");
    });

    try
    {
        Console.WriteLine("Connecting to hub...");
        await connection.StartAsync();
        Console.WriteLine("Connected!");

        if (int.TryParse(busIdInput, out int busId))
        {
            await connection.InvokeAsync("SubscribeToBus", busId);
            Console.WriteLine($"Subscribed to bus {busId}");
        }
        else
        {
            await connection.InvokeAsync("SubscribeToAllBuses");
            Console.WriteLine("Subscribed to all buses");
        }

        Console.WriteLine("\nVenter på GPS updates... (tryk ENTER for at stoppe)\n");
        Console.ReadLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    finally
    {
        await connection.StopAsync();
    }
}
