using SmartMobilityApp.ViewModels;

namespace SmartMobilityApp.Views;

public partial class DriverPage : ContentPage
{
    private readonly DriverViewModel _viewModel;

    public DriverPage(DriverViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadBusesCommand.ExecuteAsync(null);
    }
}
