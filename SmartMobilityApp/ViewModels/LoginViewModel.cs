using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartMobilityApp.Models;
using SmartMobilityApp.Services;

namespace SmartMobilityApp.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Indtast venligst email og adgangskode";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var response = await _authService.LoginAsync(Email, Password);

            if (response.Success && response.User != null)
            {
                await NavigateToRolePage(response.User.Role);
            }
            else
            {
                ErrorMessage = response.Error ?? "Login fejlede";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fejl: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task CheckExistingSessionAsync()
    {
        if (await _authService.TryRestoreSessionAsync())
        {
            if (_authService.CurrentUser != null)
            {
                await NavigateToRolePage(_authService.CurrentUser.Role);
            }
        }
    }

    private static async Task NavigateToRolePage(UserRole role)
    {
        var route = role switch
        {
            UserRole.Driver => "//DriverPage",
            UserRole.Admin => "//PassengerPage",
            _ => "//PassengerPage"
        };

        await Shell.Current.GoToAsync(route);
    }
}
