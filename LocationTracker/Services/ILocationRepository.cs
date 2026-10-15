using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>
/// Persists and retrieves the user's recorded locations.
/// </summary>
public interface ILocationRepository
{
    /// <summary>Saves a single location to storage.</summary>
    Task SaveAsync(LocationPoint point);

    /// <summary>Saves several locations in one transaction.</summary>
    Task SaveAllAsync(IEnumerable<LocationPoint> points);

    /// <summary>Returns every stored location, oldest first.</summary>
    Task<IReadOnlyList<LocationPoint>> GetAllAsync();

    /// <summary>Deletes all stored locations.</summary>
    Task ClearAsync();
}
