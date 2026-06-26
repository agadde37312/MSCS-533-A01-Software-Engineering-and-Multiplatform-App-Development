// Services/LocationService.cs
// Wraps the MAUI Geolocation API and emits periodic location updates.
// Raises the LocationUpdated event whenever a new reading is obtained so
// that any subscriber (e.g., the ViewModel) can react without polling.

using LocationHeatMap.Models;
using Microsoft.Maui.Devices.Sensors;

namespace LocationHeatMap.Services
{
    /// <summary>
    /// Provides continuous GPS tracking using the MAUI Geolocation API.
    /// Automatically saves each reading to the <see cref="DatabaseService"/>.
    /// </summary>
    public class LocationService
    {
        // -----------------------------------------------------------------
        // Dependencies & state
        // -----------------------------------------------------------------

        private readonly DatabaseService _databaseService;
        private CancellationTokenSource? _trackingCts;
        private bool _isTracking;

        // Interval between successive location polls.
        private static readonly TimeSpan TrackingInterval = TimeSpan.FromSeconds(10);

        // Minimum accuracy requested from the OS location provider.
        private const GeolocationAccuracy RequestedAccuracy = GeolocationAccuracy.Best;

        // -----------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------

        /// <summary>Raised on the UI thread every time a new location is available.</summary>
        public event EventHandler<LocationEntry>? LocationUpdated;

        /// <summary>Raised when tracking starts or stops.</summary>
        public event EventHandler<bool>? TrackingStateChanged;

        // -----------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------

        /// <summary>Whether the service is currently polling for locations.</summary>
        public bool IsTracking => _isTracking;

        // -----------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------

        public LocationService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // -----------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Begins the location tracking loop in the background.
        /// Does nothing if tracking is already active.
        /// </summary>
        public async Task StartTrackingAsync()
        {
            if (_isTracking)
                return;

            // Verify the user has granted location permission before starting.
            var permissionStatus = await CheckAndRequestPermissionsAsync();
            if (permissionStatus != PermissionStatus.Granted)
                throw new PermissionException("Location permission was not granted.");

            _isTracking = true;
            _trackingCts = new CancellationTokenSource();
            TrackingStateChanged?.Invoke(this, _isTracking);

            // Run the polling loop on a background thread so the UI stays responsive.
            _ = Task.Run(() => TrackingLoopAsync(_trackingCts.Token));
        }

        /// <summary>
        /// Stops the background tracking loop gracefully.
        /// </summary>
        public void StopTracking()
        {
            if (!_isTracking)
                return;

            _trackingCts?.Cancel();
            _trackingCts?.Dispose();
            _trackingCts = null;
            _isTracking = false;
            TrackingStateChanged?.Invoke(this, _isTracking);
        }

        /// <summary>
        /// Performs a one-shot location read without starting continuous tracking.
        /// Returns null if the platform cannot provide a reading within the timeout.
        /// </summary>
        public async Task<LocationEntry?> GetCurrentLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(RequestedAccuracy, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);
                return location is null ? null : MapToEntry(location);
            }
            catch (FeatureNotSupportedException)
            {
                // GPS not available on this device.
                return null;
            }
            catch (FeatureNotEnabledException)
            {
                // User has disabled location services in system settings.
                return null;
            }
            catch (PermissionException)
            {
                return null;
            }
        }

        // -----------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Core polling loop: calls the platform GPS API at regular intervals
        /// and persists each valid reading.
        /// </summary>
        private async Task TrackingLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var request = new GeolocationRequest(RequestedAccuracy, TimeSpan.FromSeconds(15));
                    var location = await Geolocation.Default.GetLocationAsync(request, token);

                    if (location is not null)
                    {
                        var entry = MapToEntry(location);

                        // Persist to SQLite on a background thread – no UI work needed here.
                        await _databaseService.SaveLocationAsync(entry);

                        // Marshal the event back to the UI thread so bindings update safely.
                        MainThread.BeginInvokeOnMainThread(
                            () => LocationUpdated?.Invoke(this, entry));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when StopTracking() is called – exit cleanly.
                    break;
                }
                catch (Exception ex)
                {
                    // Log unexpected errors but keep the loop running.
                    Console.WriteLine($"[LocationService] Error: {ex.Message}");
                }

                // Wait for the next poll interval (or until cancellation).
                await Task.Delay(TrackingInterval, token).ContinueWith(_ => { });
            }
        }

        /// <summary>
        /// Maps a MAUI <see cref="Location"/> to our <see cref="LocationEntry"/> model.
        /// </summary>
        private static LocationEntry MapToEntry(Location location) => new()
        {
            Latitude  = location.Latitude,
            Longitude = location.Longitude,
            Accuracy  = location.Accuracy ?? 0,
            Altitude  = location.Altitude,
            Speed     = location.Speed,
            Timestamp = location.Timestamp.UtcDateTime
        };

        /// <summary>
        /// Requests location permission if not already granted and returns the result.
        /// </summary>
        private static async Task<PermissionStatus> CheckAndRequestPermissionsAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
                return status;

            // Prompt the user if the permission hasn't been granted yet.
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status;
        }
    }
}
