using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using SmartMobilityApp.ViewModels;

namespace SmartMobilityApp.Views;

public partial class BusMapPage : ContentPage
{
    private readonly BusMapViewModel _viewModel;
    private MemoryLayer? _busLayer;
    private MemoryLayer? _stopLayer;
    private bool _hasZoomed = false;

    public BusMapPage(BusMapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.BusPositionsUpdated += OnBusPositionsUpdated;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _hasZoomed = false;
        SetupMap();
        await _viewModel.InitializeCommand.ExecuteAsync(null);
        UpdateMapMarkers();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.BusPositionsUpdated -= OnBusPositionsUpdated;
    }

    private void SetupMap()
    {
        if (MapControl.Map.Layers.Count > 0) return;

        MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        _stopLayer = new MemoryLayer
        {
            Name = "StopLayer",
            Style = null
        };
        MapControl.Map.Layers.Add(_stopLayer);

        _busLayer = new MemoryLayer
        {
            Name = "BusLayer",
            Style = null
        };
        MapControl.Map.Layers.Add(_busLayer);
    }

    private void OnBusPositionsUpdated(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => UpdateMapMarkers());
    }

    private void UpdateMapMarkers()
    {
        UpdateStopMarkers();
        UpdateBusMarkers();
    }

    private void UpdateStopMarkers()
    {
        if (_stopLayer == null || _viewModel.StopEtas == null) return;

        var features = new List<PointFeature>();

        foreach (var stop in _viewModel.StopEtas)
        {
            if (_viewModel.RouteDetail?.Stops != null)
            {
                var routeStop = _viewModel.RouteDetail.Stops.FirstOrDefault(s => s.StopId == stop.StopId);
            }
        }

        _stopLayer.Features = features;
        _stopLayer.DataHasChanged();
    }

    private void UpdateBusMarkers()
    {
        if (_busLayer == null) return;

        var features = new List<PointFeature>();
        MPoint? firstPoint = null;

        foreach (var bus in _viewModel.BusesOnRoute)
        {
            var sphericalCoord = SphericalMercator.FromLonLat(bus.Longitude, bus.Latitude);
            var point = new MPoint(sphericalCoord.x, sphericalCoord.y);

            firstPoint ??= point;

            var isSelected = _viewModel.SelectedBus?.BusId == bus.BusId;

            var busFeature = new PointFeature(point)
            {
                Styles = new[]
                {
                    new SymbolStyle
                    {
                        SymbolScale = isSelected ? 0.6 : 0.4,
                        Fill = new Mapsui.Styles.Brush(isSelected
                            ? Mapsui.Styles.Color.FromString("#512BD4")
                            : Mapsui.Styles.Color.FromString("#9575CD")),
                        Outline = new Pen(Mapsui.Styles.Color.White, 2)
                    }
                }
            };

            features.Add(busFeature);
        }

        _busLayer.Features = features;
        _busLayer.DataHasChanged();

        if (firstPoint != null && !_hasZoomed)
        {
            MapControl.Map.Navigator.CenterOnAndZoomTo(firstPoint, MapControl.Map.Navigator.Resolutions[15]);
            _hasZoomed = true;
        }
    }
}
