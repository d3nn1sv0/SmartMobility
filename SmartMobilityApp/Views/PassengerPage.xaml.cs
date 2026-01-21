using SmartMobilityApp.ViewModels;

namespace SmartMobilityApp.Views;

public partial class PassengerPage : ContentPage
{
    private readonly PassengerViewModel _viewModel;

    public PassengerPage(PassengerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshCommand.ExecuteAsync(null);
    }
}
