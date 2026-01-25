namespace SmartMobilityApp.Services.Interfaces;

public interface IAuthService
{
    bool IsLoggedIn { get; }
    UserDto? CurrentUser { get; }
    Task<AuthResponseDto> LoginAsync(string email, string password);
    Task<AuthResponseDto> RegisterAsync(string email, string password, string? name);
    Task LogoutAsync();
    Task<bool> TryRestoreSessionAsync();
}
