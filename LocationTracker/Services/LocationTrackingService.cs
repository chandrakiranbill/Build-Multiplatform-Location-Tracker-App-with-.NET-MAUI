using LocationTracker.Models;
using Microsoft.Extensions.Logging;

namespace LocationTracker.Services;

/// <summary>
/// Tracks the user's position with the MAUI <see cref="IGeolocation"/> API and saves
/// each meaningful movement to the <see cref="ILocationRepository"/>.
/// </summary>
public sealed class LocationTrackingService : ILocationTrackingService
{
    /// <summary>How often the OS should deliver updates, at most.</summary>
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Readings closer than this to the previous saved point are ignored, so standing
    /// still does not flood the database with duplicate points (GPS jitter).
    /// </summary>
    private const double MinimumDistanceMeters = 10;

    private readonly IGeolocation _geolocation;
    private readonly ILocationRepository _repository;
    private readonly ILogger<LocationTrackingService> _logger;

    private Location? _lastSavedLocation;

    public LocationTrackingService(
        IGeolocation geolocation,
        ILocationRepository repository,
        ILogger<LocationTrackingService> logger)
    {
        _geolocation = geolocation;
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<LocationPoint>? LocationSaved;

    /// <inheritdoc />
    public event EventHandler<string>? TrackingFailed;

    /// <inheritdoc />
    public bool IsTracking => _geolocation.IsListeningForeground;

    /// <inheritdoc />
    public async Task<bool> StartAsync()
    {
        if (IsTracking)
        {
            return true;
        }

        if (!await EnsurePermissionAsync())
        {
            return false;
        }

        _geolocation.LocationChanged += OnLocationChanged;
        _geolocation.ListeningFailed += OnListeningFailed;

        var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, UpdateInterval);
        var started = await _geolocation.StartListeningForegroundAsync(request);

        if (!started)
        {
            Unsubscribe();
        }

        _logger.LogInformation("Location tracking started: {Started}", started);
        return started;
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (IsTracking)
        {
            _geolocation.StopListeningForeground();
        }

        Unsubscribe();
        _logger.LogInformation("Location tracking stopped");
    }

    /// <summary>
    /// Checks the "when in use" location permission and asks the user for it if needed.
    /// Permission prompts must be shown on the UI thread.
    /// </summary>
    private static Task<bool> EnsurePermissionAsync() =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            return status == PermissionStatus.Granted;
        });

    /// <summary>
    /// Handles a new GPS reading: filters out tiny movements, saves it and notifies listeners.
    /// </summary>
    private async void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        // async void is acceptable here because this is an event handler;
        // all exceptions are caught so they cannot crash the app.
        try
        {
            var location = e.Location;

            if (_lastSavedLocation is not null)
            {
                var distanceKm = Location.CalculateDistance(
                    _lastSavedLocation, location, DistanceUnits.Kilometers);

                if (distanceKm * 1000 < MinimumDistanceMeters)
                {
                    return;
                }
            }

            var point = LocationPoint.FromLocation(location);
            await _repository.SaveAsync(point);
            _lastSavedLocation = location;

            LocationSaved?.Invoke(this, point);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save location");
        }
    }

    /// <summary>
    /// Called by the OS when it can no longer deliver locations.
    /// </summary>
    private void OnListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
    {
        _logger.LogWarning("Location listening failed: {Error}", e.Error);
        Unsubscribe();
        TrackingFailed?.Invoke(this, $"Tracking stopped: {e.Error}");
    }

    private void Unsubscribe()
    {
        _geolocation.LocationChanged -= OnLocationChanged;
        _geolocation.ListeningFailed -= OnListeningFailed;
    }
}
