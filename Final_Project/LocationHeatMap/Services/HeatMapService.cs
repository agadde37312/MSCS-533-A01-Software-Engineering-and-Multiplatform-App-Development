// Services/HeatMapService.cs
// Converts raw GPS location entries into aggregated heat map points.
// Uses a grid-based clustering algorithm: the globe is divided into equal-
// sized cells and every entry that falls in the same cell is merged into a
// single HeatMapPoint whose weight equals the sample count.

using LocationHeatMap.Models;

namespace LocationHeatMap.Services
{
    /// <summary>
    /// Converts a list of raw <see cref="LocationEntry"/> records into
    /// spatially aggregated <see cref="HeatMapPoint"/> objects suitable for
    /// rendering on a heat map overlay.
    /// </summary>
    public class HeatMapService
    {
        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        /// <summary>
        /// Size of each clustering grid cell in decimal degrees.
        /// ~111 m per degree at the equator; 0.0005° ≈ 55 m.
        /// Adjust to trade off detail vs. performance.
        /// </summary>
        private const double GridCellSize = 0.0005;

        // -----------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Aggregates a collection of raw GPS entries into weighted heat map points.
        /// </summary>
        /// <param name="entries">
        ///     All <see cref="LocationEntry"/> records retrieved from the database.
        /// </param>
        /// <returns>
        ///     A list of <see cref="HeatMapPoint"/> objects ready for rendering,
        ///     or an empty list when <paramref name="entries"/> is null or empty.
        /// </returns>
        public List<HeatMapPoint> GenerateHeatMapPoints(IEnumerable<LocationEntry>? entries)
        {
            if (entries is null)
                return [];

            // ---- Step 1: cluster entries into grid cells -----------------
            // The dictionary key is (gridRow, gridCol) encoded as a tuple.
            var clusters = new Dictionary<(int Row, int Col), List<LocationEntry>>();

            foreach (var entry in entries)
            {
                var key = GetGridKey(entry.Latitude, entry.Longitude);

                if (!clusters.TryGetValue(key, out var list))
                {
                    list = [];
                    clusters[key] = list;
                }

                list.Add(entry);
            }

            // ---- Step 2: compute the centroid and weight for each cluster ----
            var points = clusters.Values
                .Select(cluster => new HeatMapPoint
                {
                    Latitude  = cluster.Average(e => e.Latitude),
                    Longitude = cluster.Average(e => e.Longitude),
                    Weight    = cluster.Count
                })
                .ToList();

            // ---- Step 3: normalise weights so the busiest cell = 1.0 -------
            if (points.Count > 0)
            {
                int maxWeight = points.Max(p => p.Weight);

                foreach (var point in points)
                    point.NormalisedIntensity = (double)point.Weight / maxWeight;
            }

            return points;
        }

        /// <summary>
        /// Returns the colour that should represent a given normalised intensity.
        /// The gradient goes: blue (cold) → green → yellow → red (hot).
        /// </summary>
        /// <param name="intensity">Value in [0, 1].</param>
        public static Color IntensityToColor(double intensity)
        {
            // Clamp defensively.
            intensity = Math.Clamp(intensity, 0.0, 1.0);

            // Four-stop gradient: 0=blue, 0.33=green, 0.66=yellow, 1=red.
            return intensity switch
            {
                <= 0.33 => InterpolateColor(Colors.Blue,   Colors.Green,  intensity / 0.33),
                <= 0.66 => InterpolateColor(Colors.Green,  Colors.Yellow, (intensity - 0.33) / 0.33),
                _       => InterpolateColor(Colors.Yellow, Colors.Red,    (intensity - 0.66) / 0.34),
            };
        }

        /// <summary>
        /// Computes the radius in metres that a heat circle should have,
        /// scaling between 30 m (coldest) and 200 m (hottest).
        /// </summary>
        /// <param name="intensity">Normalised intensity in [0, 1].</param>
        public static double IntensityToRadius(double intensity)
        {
            const double MinRadius = 30.0;
            const double MaxRadius = 200.0;
            return MinRadius + (MaxRadius - MinRadius) * intensity;
        }

        // -----------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Converts GPS coordinates to a discrete grid-cell key.
        /// Both latitude and longitude are divided into bins of <see cref="GridCellSize"/>.
        /// </summary>
        private static (int Row, int Col) GetGridKey(double latitude, double longitude)
        {
            int row = (int)Math.Floor(latitude  / GridCellSize);
            int col = (int)Math.Floor(longitude / GridCellSize);
            return (row, col);
        }

        /// <summary>
        /// Linearly interpolates between two MAUI <see cref="Color"/> values.
        /// </summary>
        /// <param name="from">Start colour (t = 0).</param>
        /// <param name="to">End colour (t = 1).</param>
        /// <param name="t">Blend factor in [0, 1].</param>
        private static Color InterpolateColor(Color from, Color to, double t)
        {
            float ft = (float)Math.Clamp(t, 0.0, 1.0);
            return new Color(
                from.Red   + (to.Red   - from.Red)   * ft,
                from.Green + (to.Green - from.Green)  * ft,
                from.Blue  + (to.Blue  - from.Blue)   * ft,
                0.55f  // fixed alpha so the map is always partially visible beneath
            );
        }
    }
}
