// Services/DatabaseService.cs
// Manages all SQLite operations for persisting location entries.
// Uses the sqlite-net-pcl library so the same code runs on Android, iOS,
// macOS, and Windows without platform-specific adaptations.

using SQLite;
using LocationHeatMap.Models;

namespace LocationHeatMap.Services
{
    /// <summary>
    /// Provides async CRUD operations for <see cref="LocationEntry"/> records.
    /// The underlying SQLite connection is initialised lazily and thread-safe.
    /// </summary>
    public class DatabaseService
    {
        // -----------------------------------------------------------------
        // Fields
        // -----------------------------------------------------------------

        private SQLiteAsyncConnection? _database;

        /// <summary>
        /// Full path to the SQLite file inside the app's local data folder.
        /// Each platform maps this to its own sandboxed storage.
        /// </summary>
        private readonly string _databasePath =
            Path.Combine(FileSystem.AppDataDirectory, "LocationHeatMap.db3");

        // -----------------------------------------------------------------
        // Initialisation
        // -----------------------------------------------------------------

        /// <summary>
        /// Ensures the connection is open and the schema is up-to-date.
        /// Called automatically before any data operation (lazy init pattern).
        /// </summary>
        private async Task InitialiseAsync()
        {
            if (_database is not null)
                return;

            // Open (or create) the database file with read/write permissions.
            _database = new SQLiteAsyncConnection(
                _databasePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            // CreateTableAsync is idempotent – it adds missing columns but
            // never drops existing data, so it is safe to call on every launch.
            await _database.CreateTableAsync<LocationEntry>();
        }

        // -----------------------------------------------------------------
        // Write operations
        // -----------------------------------------------------------------

        /// <summary>
        /// Persists a new location reading to the database.
        /// </summary>
        /// <param name="entry">The location entry to save.</param>
        /// <returns>The number of rows inserted (always 1 on success).</returns>
        public async Task<int> SaveLocationAsync(LocationEntry entry)
        {
            await InitialiseAsync();
            return await _database!.InsertAsync(entry);
        }

        /// <summary>
        /// Removes all stored location entries (reset / privacy wipe).
        /// </summary>
        public async Task<int> ClearAllLocationsAsync()
        {
            await InitialiseAsync();
            return await _database!.DeleteAllAsync<LocationEntry>();
        }

        // -----------------------------------------------------------------
        // Read operations
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns every stored location entry, newest first.
        /// </summary>
        public async Task<List<LocationEntry>> GetAllLocationsAsync()
        {
            await InitialiseAsync();
            return await _database!
                .Table<LocationEntry>()
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Returns entries recorded between two UTC timestamps.
        /// </summary>
        /// <param name="from">Inclusive start of the time window.</param>
        /// <param name="to">Inclusive end of the time window.</param>
        public async Task<List<LocationEntry>> GetLocationsInRangeAsync(
            DateTime from, DateTime to)
        {
            await InitialiseAsync();
            return await _database!
                .Table<LocationEntry>()
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Returns the total number of recorded location samples.
        /// </summary>
        public async Task<int> GetEntryCountAsync()
        {
            await InitialiseAsync();
            return await _database!.Table<LocationEntry>().CountAsync();
        }

        /// <summary>
        /// Returns the most recently recorded entry, or null if the database
        /// is empty.
        /// </summary>
        public async Task<LocationEntry?> GetLatestLocationAsync()
        {
            await InitialiseAsync();
            return await _database!
                .Table<LocationEntry>()
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();
        }
    }
}
