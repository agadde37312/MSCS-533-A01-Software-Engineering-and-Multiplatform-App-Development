// Models/LocationEntry.cs
// Represents a single location data point stored in the SQLite database.
// Each entry captures GPS coordinates, accuracy, altitude, and timestamp.

using SQLite;

namespace LocationHeatMap.Models
{
    /// <summary>
    /// Entity model for a recorded GPS location point.
    /// Maps directly to the LocationEntries table in the SQLite database.
    /// </summary>
    [Table("LocationEntries")]
    public class LocationEntry
    {
        /// <summary>
        /// Auto-incremented primary key managed by SQLite.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// WGS-84 latitude in decimal degrees (-90 to +90).
        /// </summary>
        [NotNull]
        public double Latitude { get; set; }

        /// <summary>
        /// WGS-84 longitude in decimal degrees (-180 to +180).
        /// </summary>
        [NotNull]
        public double Longitude { get; set; }

        /// <summary>
        /// Horizontal accuracy radius in metres. Lower is better.
        /// </summary>
        public double Accuracy { get; set; }

        /// <summary>
        /// Altitude above sea level in metres (may be null if unavailable).
        /// </summary>
        public double? Altitude { get; set; }

        /// <summary>
        /// UTC timestamp of when the location was recorded.
        /// </summary>
        [NotNull]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Speed in metres per second (may be null if unavailable).
        /// </summary>
        public double? Speed { get; set; }

        /// <summary>
        /// Returns a human-readable summary of this location entry.
        /// </summary>
        public override string ToString() =>
            $"({Latitude:F5}, {Longitude:F5}) @ {Timestamp:HH:mm:ss}";
    }
}
