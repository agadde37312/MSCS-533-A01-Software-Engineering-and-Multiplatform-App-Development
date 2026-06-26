// Models/HeatMapPoint.cs
// Represents an aggregated heat map data point, combining multiple nearby
// GPS readings into a single weighted point for rendering on the map.

namespace LocationHeatMap.Models
{
    /// <summary>
    /// An aggregated point used for heat map visualisation.
    /// Weight drives the intensity of the heat circle drawn on the map.
    /// </summary>
    public class HeatMapPoint
    {
        /// <summary>
        /// Centre latitude of the aggregated cluster.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Centre longitude of the aggregated cluster.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Number of raw GPS samples that contributed to this point.
        /// Used to calculate the heat intensity (colour + radius).
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// Normalised intensity in the range [0, 1].
        /// Computed after all points are collected so the most-visited
        /// location always reaches intensity 1.
        /// </summary>
        public double NormalisedIntensity { get; set; }

        /// <summary>
        /// Returns a Microsoft.Maui.Maps.Location for use with MAUI Maps APIs.
        /// </summary>
        public Location ToMauiLocation() => new Location(Latitude, Longitude);
    }
}
