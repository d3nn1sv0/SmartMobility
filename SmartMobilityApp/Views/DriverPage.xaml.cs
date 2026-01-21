using SmartMobilityApp.ViewModels;

namespace SmartMobilityApp.Views;

public partial class DriverPage : ContentPage
{
    public DriverPage(DriverViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
