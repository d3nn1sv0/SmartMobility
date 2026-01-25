namespace SmartMobilityApp.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Indtast venligst en email";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Indtast venligst en adgangskode";
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Adgangskode skal være mindst 6 tegn";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Adgangskoderne matcher ikke";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var response = await _authService.RegisterAsync(
                Email,
                Password,
                string.IsNullOrWhiteSpace(Name) ? null : Name);

            if (response.Success)
            {
                await Shell.Current.GoToAsync("//PassengerPage");
            }
            else
            {
                ErrorMessage = response.Error ?? "Registrering fejlede";
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

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
