// Views/MainPage.xaml.cs
// Code-behind for MainPage.xaml.
// Responsible for wiring the map's viewport to the HeatMapOverlay so the
// overlay knows which GPS rectangle is currently visible. All business logic
// lives in MainViewModel; this file handles only view-specific concerns.

using LocationHeatMap.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace LocationHeatMap.Views
{
    /// <summary>
    /// Code-behind for the main application screen.
    /// Sets the BindingContext to the injected <see cref="MainViewModel"/>,
    /// and keeps the <see cref="HeatMapOverlay"/> in sync with the map viewport.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        // -----------------------------------------------------------------
        // Fields
        // -----------------------------------------------------------------

        private readonly MainViewModel _viewModel;

        // Last known map region – used to detect real viewport changes.
        private MapSpan? _lastRegion;

        // -----------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel    = viewModel;
            BindingContext = viewModel;

            // Subscribe to map movement so the heat overlay stays aligned.
            MainMap.PropertyChanged += MainMap_PropertyChanged;
        }

        // -----------------------------------------------------------------
        // Lifecycle overrides
        // -----------------------------------------------------------------

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Pan to the ViewModel's stored centre position when the page shows.
            var initialSpan = MapSpan.FromCenterAndRadius(
                new Location(_viewModel.MapLatitude, _viewModel.MapLongitude),
                Distance.FromKilometers(1));

            MainMap.MoveToRegion(initialSpan);
            SyncOverlayViewport(initialSpan);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MainMap.PropertyChanged -= MainMap_PropertyChanged;
        }

        // -----------------------------------------------------------------
        // Map viewport synchronisation
        // -----------------------------------------------------------------

        /// <summary>
        /// Fires whenever any property on the Map control changes.
        /// We listen for VisibleRegion changes and push the new viewport
        /// bounds to the HeatMapOverlay so it redraws at the correct scale.
        /// </summary>
        private void MainMap_PropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Microsoft.Maui.Controls.Maps.Map.VisibleRegion))
                return;

            var region = MainMap.VisibleRegion;
            if (region is null || region == _lastRegion)
                return;

            _lastRegion = region;
            SyncOverlayViewport(region);
        }

        /// <summary>
        /// Updates the four bounding-box properties on the HeatMapOverlay so
        /// it can convert GPS coordinates to screen pixel positions.
        /// </summary>
        private void SyncOverlayViewport(MapSpan region)
        {
            // Calculate the four edges from the centre + half-span values.
            double halfLat = region.LatitudeDegrees  / 2.0;
            double halfLon = region.LongitudeDegrees / 2.0;

            HeatOverlay.NorthWestLatitude  = region.Center.Latitude  + halfLat;
            HeatOverlay.NorthWestLongitude = region.Center.Longitude - halfLon;
            HeatOverlay.SouthEastLatitude  = region.Center.Latitude  - halfLat;
            HeatOverlay.SouthEastLongitude = region.Center.Longitude + halfLon;

            // Trigger a repaint of the overlay canvas.
            HeatOverlay.Invalidate();
        }
    }
}
