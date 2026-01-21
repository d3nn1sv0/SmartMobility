using SmartMobilityApp.Models;

namespace SmartMobilityApp.Services;

public interface IAuthService
{
    bool IsLoggedIn { get; }
    UserDto? CurrentUser { get; }
    Task<AuthResponseDto> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<bool> TryRestoreSessionAsync();
}
