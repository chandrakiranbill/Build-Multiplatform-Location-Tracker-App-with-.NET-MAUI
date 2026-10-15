namespace LocationTracker.Controls;

/// <summary>
/// One cell of the heat map grid: where it is, how many points fell in it,
/// and its intensity relative to the busiest cell (0..1).
/// </summary>
/// <param name="Center">Average position of the points in the cell.</param>
/// <param name="Count">Number of recorded points in the cell.</param>
/// <param name="Intensity">Count divided by the largest count in any cell (0..1].</param>
public sealed record HeatCell(Location Center, int Count, double Intensity);

/// <summary>
/// Pure, UI-independent logic that turns raw locations into heat map cells.
/// Kept separate from the control so it is easy to unit-test.
/// </summary>
public static class HeatMapCalculator
{
    /// <summary>Approximate length of one degree of latitude in metres.</summary>
    private const double MetersPerDegreeLatitude = 111_320d;

    /// <summary>
    /// Groups locations into square cells of roughly <paramref name="cellSizeMeters"/>
    /// and counts how many fall in each one.
    /// </summary>
    /// <param name="locations">The recorded locations.</param>
    /// <param name="cellSizeMeters">Width/height of each grid cell in metres.</param>
    /// <returns>One <see cref="HeatCell"/> per non-empty grid cell.</returns>
    public static IReadOnlyList<HeatCell> Calculate(IEnumerable<Location> locations, double cellSizeMeters)
    {
        ArgumentNullException.ThrowIfNull(locations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSizeMeters);

        var points = locations.ToList();
        if (points.Count == 0)
        {
            return [];
        }

        // Degrees of longitude shrink towards the poles, so scale the longitude
        // cell size using the average latitude of the data set.
        var averageLatitude = points.Average(p => p.Latitude);
        var cosLatitude = Math.Max(Math.Cos(averageLatitude * Math.PI / 180d), 0.01);

        var latStep = cellSizeMeters / MetersPerDegreeLatitude;
        var lonStep = cellSizeMeters / (MetersPerDegreeLatitude * cosLatitude);

        var groups = points
            .GroupBy(p => (Row: (long)Math.Floor(p.Latitude / latStep),
                           Col: (long)Math.Floor(p.Longitude / lonStep)))
            .ToList();

        var maxCount = groups.Max(g => g.Count());

        return groups
            .Select(g => new HeatCell(
                Center: new Location(g.Average(p => p.Latitude), g.Average(p => p.Longitude)),
                Count: g.Count(),
                Intensity: (double)g.Count() / maxCount))
            // Draw low-intensity cells first so "hot" cells are rendered on top.
            .OrderBy(c => c.Intensity)
            .ToList();
    }

    /// <summary>
    /// Maps an intensity (0..1) to a colour on a classic heat gradient:
    /// blue → cyan → green → yellow → red.
    /// </summary>
    /// <param name="intensity">Value between 0 (cold) and 1 (hot).</param>
    /// <param name="alpha">Opacity of the resulting colour.</param>
    public static Color GetHeatColor(double intensity, float alpha = 0.6f)
    {
        // Gradient stops (position, colour).
        (double Position, Color Color)[] stops =
        [
            (0.00, Colors.Blue),
            (0.25, Colors.Cyan),
            (0.50, Colors.Lime),
            (0.75, Colors.Yellow),
            (1.00, Colors.Red),
        ];

        var t = Math.Clamp(intensity, 0d, 1d);

        for (var i = 1; i < stops.Length; i++)
        {
            if (t <= stops[i].Position)
            {
                var (startPos, start) = stops[i - 1];
                var (endPos, end) = stops[i];
                var local = (float)((t - startPos) / (endPos - startPos));

                return new Color(
                    Lerp(start.Red, end.Red, local),
                    Lerp(start.Green, end.Green, local),
                    Lerp(start.Blue, end.Blue, local),
                    alpha);
            }
        }

        return stops[^1].Color.WithAlpha(alpha);
    }

    private static float Lerp(float a, float b, float t) => a + ((b - a) * t);
}
