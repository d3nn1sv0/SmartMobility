using SmartMobilityApp.Services.Interfaces;

namespace SmartMobilityApp.Services;

public class AuthService : IAuthService
{
    private readonly IApiService _apiService;
    private const string TokenKey = "auth_token";
    private const string UserIdKey = "user_id";

    public bool IsLoggedIn => CurrentUser != null;
    public UserDto? CurrentUser { get; private set; }

    public AuthService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<AuthResponseDto> LoginAsync(string email, string password)
    {
        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var response = await _apiService.PostAsync<LoginDto, AuthResponseDto>("auth/login", loginDto);

        if (response == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Error = "Kunne ikke forbinde til serveren"
            };
        }

        if (response.Success && response.Token != null && response.User != null)
        {
            await SecureStorage.SetAsync(TokenKey, response.Token);
            await SecureStorage.SetAsync(UserIdKey, response.User.Id.ToString());
            _apiService.SetAuthToken(response.Token);
            CurrentUser = response.User;
        }

        return response;
    }

    public async Task<AuthResponseDto> RegisterAsync(string email, string password, string? name)
    {
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            Name = name
        };

        var response = await _apiService.PostAsync<RegisterDto, AuthResponseDto>("auth/register", registerDto);

        if (response == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Error = "Kunne ikke forbinde til serveren"
            };
        }

        if (response.Success && response.Token != null && response.User != null)
        {
            await SecureStorage.SetAsync(TokenKey, response.Token);
            await SecureStorage.SetAsync(UserIdKey, response.User.Id.ToString());
            _apiService.SetAuthToken(response.Token);
            CurrentUser = response.User;
        }

        return response;
    }

    public async Task LogoutAsync()
    {
        SecureStorage.Remove(TokenKey);
        SecureStorage.Remove(UserIdKey);
        _apiService.SetAuthToken(null);
        CurrentUser = null;
        await Task.CompletedTask;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync(TokenKey);

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            _apiService.SetAuthToken(token);

            var user = await _apiService.GetAsync<UserDto>("auth/me");

            if (user != null)
            {
                CurrentUser = user;
                return true;
            }

            await LogoutAsync();
            return false;
        }
        catch
        {
            return false;
        }
    }
}
