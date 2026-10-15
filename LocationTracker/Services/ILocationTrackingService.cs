using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>
/// Listens to the device's GPS and stores each new position.
/// </summary>
public interface ILocationTrackingService
{
    /// <summary>Raised (on a background thread) after a new position has been saved.</summary>
    event EventHandler<LocationPoint>? LocationSaved;

    /// <summary>Raised when tracking stops unexpectedly (e.g. GPS turned off).</summary>
    event EventHandler<string>? TrackingFailed;

    /// <summary>Gets a value indicating whether location updates are being received.</summary>
    bool IsTracking { get; }

    /// <summary>
    /// Requests location permission (if needed) and starts listening for updates.
    /// </summary>
    /// <returns><c>true</c> if tracking started; <c>false</c> if permission was denied.</returns>
    Task<bool> StartAsync();

    /// <summary>Stops listening for location updates.</summary>
    void Stop();
}
