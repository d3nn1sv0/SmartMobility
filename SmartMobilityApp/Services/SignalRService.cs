namespace SmartMobilityApp.Services;

public class SignalRService : ISignalRService, IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public event EventHandler<NextStopNotificationDto>? NextStopApproaching;
    public event EventHandler<BusPositionUpdateDto>? BusPositionUpdated;
    public event EventHandler<string>? ConnectionStateChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync()
    {
        if (_hubConnection != null)
        {
            await DisconnectAsync();
        }

        var token = await SecureStorage.GetAsync("auth_token");
        if (string.IsNullOrEmpty(token))
        {
            System.Diagnostics.Debug.WriteLine("SignalR: No auth token available");
            return;
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Configuration.Constants.Api.HubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<BusPositionUpdateDto>("BusPositionUpdated", position =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BusPositionUpdated?.Invoke(this, position);
            });
        });

        _hubConnection.On<NextStopNotificationDto>("NextStopApproaching", notification =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NextStopApproaching?.Invoke(this, notification);
            });
        });

        _hubConnection.Reconnecting += error =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionStateChanged?.Invoke(this, "Reconnecting");
            });
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += async connectionId =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionStateChanged?.Invoke(this, "Connected");
            });
            System.Diagnostics.Debug.WriteLine("SignalR: Reconnected, re-subscribing to all buses");
            await _hubConnection.InvokeAsync("SubscribeToAllBuses");
        };

        _hubConnection.Closed += error =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionStateChanged?.Invoke(this, "Disconnected");
            });
            return Task.CompletedTask;
        };

        try
        {
            await _hubConnection.StartAsync();
            ConnectionStateChanged?.Invoke(this, "Connected");
            System.Diagnostics.Debug.WriteLine("SignalR: Connected successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SignalR: Connection failed - {ex.Message}");
            ConnectionStateChanged?.Invoke(this, "Disconnected");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR: Disconnect error - {ex.Message}");
            }
            finally
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }
    }

    public async Task SubscribeToBusAsync(int busId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("SubscribeToBus", busId);
                System.Diagnostics.Debug.WriteLine($"SignalR: Subscribed to bus {busId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR: Subscribe to bus failed - {ex.Message}");
            }
        }
    }

    public async Task UnsubscribeFromBusAsync(int busId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromBus", busId);
                System.Diagnostics.Debug.WriteLine($"SignalR: Unsubscribed from bus {busId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR: Unsubscribe from bus failed - {ex.Message}");
            }
        }
    }

    public async Task SubscribeToAllBusesAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("SubscribeToAllBuses");
                    System.Diagnostics.Debug.WriteLine("SignalR: Subscribed to all buses");
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SignalR: Subscribe to all buses failed - {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SignalR: Not connected, waiting... (attempt {i + 1}/5)");
                await Task.Delay(500);
            }
        }
        System.Diagnostics.Debug.WriteLine("SignalR: Failed to subscribe after 5 attempts");
    }

    public async Task UnsubscribeFromAllBusesAsync()
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromAllBuses");
                System.Diagnostics.Debug.WriteLine("SignalR: Unsubscribed from all buses");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR: Unsubscribe from all buses failed - {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
