using LocationTracker.Models;
using SQLite;

namespace LocationTracker.Services;

/// <summary>
/// SQLite implementation of <see cref="ILocationRepository"/> using sqlite-net.
/// The database file lives in the app's private data directory.
/// </summary>
public sealed class LocationRepository : ILocationRepository
{
    /// <summary>File name of the SQLite database.</summary>
    private const string DatabaseFileName = "locations.db3";

    /// <summary>Open flags: read/write, create if missing, safe for multi-threaded access.</summary>
    private const SQLiteOpenFlags OpenFlags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    /// <summary>Gets the full path to the database file.</summary>
    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    /// <inheritdoc />
    public async Task SaveAsync(LocationPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        var db = await GetConnectionAsync();
        await db.InsertAsync(point);
    }

    /// <inheritdoc />
    public async Task SaveAllAsync(IEnumerable<LocationPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var db = await GetConnectionAsync();
        await db.InsertAllAsync(points, runInTransaction: true);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LocationPoint>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<LocationPoint>()
                       .OrderBy(p => p.TimestampUtc)
                       .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        var db = await GetConnectionAsync();
        await db.DeleteAllAsync<LocationPoint>();
    }

    /// <summary>
    /// Lazily opens the connection and creates the table the first time it is needed.
    /// Guarded by a semaphore so concurrent callers don't initialise twice.
    /// </summary>
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_connection is null)
            {
                var connection = new SQLiteAsyncConnection(DatabasePath, OpenFlags);
                await connection.CreateTableAsync<LocationPoint>();
                _connection = connection;
            }

            return _connection;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
