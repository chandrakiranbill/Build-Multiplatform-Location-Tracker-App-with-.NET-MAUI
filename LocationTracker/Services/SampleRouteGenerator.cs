using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>
/// Generates a realistic-looking demo route (a wavy path with some
/// repeated visits) for testing the heat map without walking around.
/// </summary>
public static class SampleRouteGenerator
{
    /// <summary>Number of points along the main path.</summary>
    private const int PathPointCount = 60;

    /// <summary>
    /// Creates sample points starting at <paramref name="start"/> heading roughly east.
    /// The start and end areas are visited repeatedly so they appear "hot" (red).
    /// </summary>
    /// <param name="start">Where the route begins.</param>
    public static IReadOnlyList<LocationPoint> Create(Location start)
    {
        ArgumentNullException.ThrowIfNull(start);

        var random = new Random();
        var now = DateTime.UtcNow;
        var points = new List<LocationPoint>();

        // Main path: ~6 km eastwards with a gentle sine-wave wiggle.
        for (var i = 0; i < PathPointCount; i++)
        {
            var progress = (double)i / PathPointCount;
            var lat = start.Latitude + (0.004 * Math.Sin(progress * Math.PI * 4));
            var lon = start.Longitude + (0.07 * progress);

            points.Add(CreatePoint(lat, lon, now.AddMinutes(i)));
        }

        // Hot spots: many readings clustered around the start (home) and end (work).
        var hotSpots = new[] { points[0], points[^1] };
        foreach (var spot in hotSpots)
        {
            for (var i = 0; i < 25; i++)
            {
                var lat = spot.Latitude + ((random.NextDouble() - 0.5) * 0.0015);
                var lon = spot.Longitude + ((random.NextDouble() - 0.5) * 0.0015);
                points.Add(CreatePoint(lat, lon, now.AddMinutes(PathPointCount + i)));
            }
        }

        return points;
    }

    private static LocationPoint CreatePoint(double latitude, double longitude, DateTime timestampUtc) => new()
    {
        Latitude = latitude,
        Longitude = longitude,
        AccuracyMeters = 5,
        TimestampUtc = timestampUtc,
    };
}
