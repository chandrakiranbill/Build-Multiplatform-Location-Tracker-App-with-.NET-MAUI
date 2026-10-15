namespace LocationTracker.Models;

/// <summary>
/// A single recorded position of the user. Each instance is one row in the
/// <c>LocationPoints</c> table of the SQLite database.
/// </summary>
[Table("LocationPoints")]
public class LocationPoint
{
    /// <summary>Gets or sets the auto-incremented primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Gets or sets the latitude in decimal degrees (WGS84).</summary>
    public double Latitude { get; set; }

    /// <summary>Gets or sets the longitude in decimal degrees (WGS84).</summary>
    public double Longitude { get; set; }

    /// <summary>Gets or sets the reported horizontal accuracy in metres, if known.</summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>Gets or sets when the position was recorded (UTC).</summary>
    [Indexed]
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Converts this record to a MAUI <see cref="Location"/> used by the map.
    /// </summary>
    public Location ToLocation() => new(Latitude, Longitude);

    /// <summary>
    /// Creates a new record from a MAUI <see cref="Location"/> reading.
    /// </summary>
    /// <param name="location">The location reported by the device.</param>
    public static LocationPoint FromLocation(Location location) => new()
    {
        Latitude = location.Latitude,
        Longitude = location.Longitude,
        AccuracyMeters = location.Accuracy,
        TimestampUtc = location.Timestamp.UtcDateTime,
    };
}
