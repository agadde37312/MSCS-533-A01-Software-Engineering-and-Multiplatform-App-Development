// ViewModels/MainViewModel.cs
// Central ViewModel for the app. Exposes commands and observable properties
// that the MainPage binds to. Follows the MVVM pattern: the View knows
// nothing about data access, and the Model has no UI concerns.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LocationHeatMap.Models;
using LocationHeatMap.Services;

namespace LocationHeatMap.ViewModels
{
    /// <summary>
    /// Drives the main screen of the Location Heat Map app.
    /// Coordinates the <see cref="LocationService"/>, <see cref="DatabaseService"/>,
    /// and <see cref="HeatMapService"/> and exposes the results to the View.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // -----------------------------------------------------------------
        // Services
        // -----------------------------------------------------------------

        private readonly LocationService  _locationService;
        private readonly DatabaseService  _databaseService;
        private readonly HeatMapService   _heatMapService;

        // -----------------------------------------------------------------
        // Backing fields
        // -----------------------------------------------------------------

        private bool   _isTracking;
        private string _statusMessage  = "Ready to track";
        private int    _entryCount;
        private string _currentCoords  = "—";
        private double _mapLatitude    = 37.7749;  // Default: San Francisco
        private double _mapLongitude   = -122.4194;
        private double _mapZoom        = 14;

        // -----------------------------------------------------------------
        // Observable properties
        // -----------------------------------------------------------------

        /// <summary>True while GPS polling is active.</summary>
        public bool IsTracking
        {
            get => _isTracking;
            private set => SetProperty(ref _isTracking, value);
        }

        /// <summary>Human-readable status shown in the status bar.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>Total number of stored location samples.</summary>
        public int EntryCount
        {
            get => _entryCount;
            private set => SetProperty(ref _entryCount, value);
        }

        /// <summary>Formatted coordinate string for the last known position.</summary>
        public string CurrentCoords
        {
            get => _currentCoords;
            private set => SetProperty(ref _currentCoords, value);
        }

        /// <summary>Map centre latitude – updated when a new location arrives.</summary>
        public double MapLatitude
        {
            get => _mapLatitude;
            set => SetProperty(ref _mapLatitude, value);
        }

        /// <summary>Map centre longitude.</summary>
        public double MapLongitude
        {
            get => _mapLongitude;
            set => SetProperty(ref _mapLongitude, value);
        }

        /// <summary>Current map zoom level (higher = more zoomed in).</summary>
        public double MapZoom
        {
            get => _mapZoom;
            set => SetProperty(ref _mapZoom, value);
        }

        /// <summary>
        /// Aggregated heat map points exposed to the view for rendering.
        /// An <see cref="ObservableCollection{T}"/> so the view refreshes automatically.
        /// </summary>
        public ObservableCollection<HeatMapPoint> HeatMapPoints { get; } = [];

        /// <summary>The most recent raw location entries (newest first).</summary>
        public ObservableCollection<LocationEntry> RecentLocations { get; } = [];

        // -----------------------------------------------------------------
        // Commands
        // -----------------------------------------------------------------

        public ICommand StartTrackingCommand  { get; }
        public ICommand StopTrackingCommand   { get; }
        public ICommand RefreshMapCommand     { get; }
        public ICommand ClearDataCommand      { get; }

        // -----------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------

        public MainViewModel(
            LocationService  locationService,
            DatabaseService  databaseService,
            HeatMapService   heatMapService)
        {
            _locationService = locationService;
            _databaseService = databaseService;
            _heatMapService  = heatMapService;

            // Wire up service events.
            _locationService.LocationUpdated      += OnLocationUpdated;
            _locationService.TrackingStateChanged += OnTrackingStateChanged;

            // Bind commands.
            StartTrackingCommand = new Command(
                execute:    async () => await StartTrackingAsync(),
                canExecute: ()         => !IsTracking);

            StopTrackingCommand = new Command(
                execute:    () => _locationService.StopTracking(),
                canExecute: () => IsTracking);

            RefreshMapCommand = new Command(
                async () => await RefreshHeatMapAsync());

            ClearDataCommand = new Command(
                async () => await ClearAllDataAsync());

            // Load any previously stored data on startup.
            _ = InitialiseAsync();
        }

        // -----------------------------------------------------------------
        // Initialisation
        // -----------------------------------------------------------------

        private async Task InitialiseAsync()
        {
            EntryCount = await _databaseService.GetEntryCountAsync();
            await RefreshHeatMapAsync();

            // Centre the map on the most recently recorded position if available.
            var latest = await _databaseService.GetLatestLocationAsync();
            if (latest is not null)
            {
                MapLatitude  = latest.Latitude;
                MapLongitude = latest.Longitude;
                CurrentCoords = $"{latest.Latitude:F5}°, {latest.Longitude:F5}°";
            }
        }

        // -----------------------------------------------------------------
        // Command implementations
        // -----------------------------------------------------------------

        private async Task StartTrackingAsync()
        {
            try
            {
                StatusMessage = "Starting GPS…";
                await _locationService.StartTrackingAsync();
            }
            catch (PermissionException)
            {
                StatusMessage = "Location permission denied. Please enable it in Settings.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not start tracking: {ex.Message}";
            }
        }

        private async Task RefreshHeatMapAsync()
        {
            StatusMessage = "Refreshing heat map…";

            var entries = await _databaseService.GetAllLocationsAsync();
            var points  = _heatMapService.GenerateHeatMapPoints(entries);

            HeatMapPoints.Clear();
            foreach (var point in points)
                HeatMapPoints.Add(point);

            // Refresh recent locations list (show last 50 only for performance).
            RecentLocations.Clear();
            foreach (var entry in entries.Take(50))
                RecentLocations.Add(entry);

            EntryCount    = entries.Count;
            StatusMessage = $"{points.Count} heat map point(s) from {EntryCount} sample(s)";
        }

        private async Task ClearAllDataAsync()
        {
            await _databaseService.ClearAllLocationsAsync();
            HeatMapPoints.Clear();
            RecentLocations.Clear();
            EntryCount    = 0;
            CurrentCoords = "—";
            StatusMessage = "All data cleared";
        }

        // -----------------------------------------------------------------
        // Event handlers
        // -----------------------------------------------------------------

        private async void OnLocationUpdated(object? sender, LocationEntry entry)
        {
            // Update the live coordinate display.
            CurrentCoords = $"{entry.Latitude:F5}°, {entry.Longitude:F5}°";
            EntryCount++;

            // Pan the map to the new position.
            MapLatitude  = entry.Latitude;
            MapLongitude = entry.Longitude;

            StatusMessage = $"Location updated at {entry.Timestamp:HH:mm:ss} UTC";

            // Regenerate heat map points so the overlay stays current.
            await RefreshHeatMapAsync();
        }

        private void OnTrackingStateChanged(object? sender, bool isTracking)
        {
            IsTracking    = isTracking;
            StatusMessage = isTracking ? "Tracking active…" : "Tracking stopped";

            // Refresh command availability (CanExecute).
            ((Command)StartTrackingCommand).ChangeCanExecute();
            ((Command)StopTrackingCommand).ChangeCanExecute();
        }

        // -----------------------------------------------------------------
        // INotifyPropertyChanged
        // -----------------------------------------------------------------

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
