// Controls/HeatMapOverlay.cs
// A transparent GraphicsView that floats above the MAUI Map and paints
// coloured heat circles for every HeatMapPoint supplied by the ViewModel.
// This approach works cross-platform because GraphicsView uses the
// Microsoft.Maui.Graphics abstraction, which is implemented by each
// platform's native 2-D drawing API.

using LocationHeatMap.Models;
using LocationHeatMap.Services;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace LocationHeatMap.Controls
{
    /// <summary>
    /// Custom control that renders a heat map overlay on top of a map.
    /// Uses <see cref="IDrawable"/> to paint radial gradient circles for
    /// each <see cref="HeatMapPoint"/> in the bound collection.
    /// </summary>
    public class HeatMapOverlay : GraphicsView, IDrawable
    {
        // -----------------------------------------------------------------
        // Bindable properties
        // -----------------------------------------------------------------

        /// <summary>
        /// The collection of heat map points to render.
        /// Changing this property or mutating the collection triggers a redraw.
        /// </summary>
        public static readonly BindableProperty HeatMapPointsProperty =
            BindableProperty.Create(
                nameof(HeatMapPoints),
                typeof(ObservableCollection<HeatMapPoint>),
                typeof(HeatMapOverlay),
                null,
                propertyChanged: OnHeatMapPointsChanged);

        public ObservableCollection<HeatMapPoint>? HeatMapPoints
        {
            get => (ObservableCollection<HeatMapPoint>?)GetValue(HeatMapPointsProperty);
            set => SetValue(HeatMapPointsProperty, value);
        }

        // -----------------------------------------------------------------
        // Map viewport properties (updated when the map pans or zooms)
        // -----------------------------------------------------------------

        /// <summary>Latitude at the top-left corner of the visible map tile.</summary>
        public static readonly BindableProperty NorthWestLatitudeProperty =
            BindableProperty.Create(nameof(NorthWestLatitude), typeof(double), typeof(HeatMapOverlay), 0.0, propertyChanged: OnViewportChanged);

        /// <summary>Longitude at the top-left corner of the visible map tile.</summary>
        public static readonly BindableProperty NorthWestLongitudeProperty =
            BindableProperty.Create(nameof(NorthWestLongitude), typeof(double), typeof(HeatMapOverlay), 0.0, propertyChanged: OnViewportChanged);

        /// <summary>Latitude at the bottom-right corner of the visible map tile.</summary>
        public static readonly BindableProperty SouthEastLatitudeProperty =
            BindableProperty.Create(nameof(SouthEastLatitude), typeof(double), typeof(HeatMapOverlay), 0.0, propertyChanged: OnViewportChanged);

        /// <summary>Longitude at the bottom-right corner of the visible map tile.</summary>
        public static readonly BindableProperty SouthEastLongitudeProperty =
            BindableProperty.Create(nameof(SouthEastLongitude), typeof(double), typeof(HeatMapOverlay), 0.0, propertyChanged: OnViewportChanged);

        public double NorthWestLatitude  { get => (double)GetValue(NorthWestLatitudeProperty);  set => SetValue(NorthWestLatitudeProperty,  value); }
        public double NorthWestLongitude { get => (double)GetValue(NorthWestLongitudeProperty); set => SetValue(NorthWestLongitudeProperty, value); }
        public double SouthEastLatitude  { get => (double)GetValue(SouthEastLatitudeProperty);  set => SetValue(SouthEastLatitudeProperty,  value); }
        public double SouthEastLongitude { get => (double)GetValue(SouthEastLongitudeProperty); set => SetValue(SouthEastLongitudeProperty, value); }

        // -----------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------

        public HeatMapOverlay()
        {
            Drawable           = this;
            BackgroundColor    = Colors.Transparent;
            InputTransparent   = true;   // Pass touch events through to the map below.
        }

        // -----------------------------------------------------------------
        // IDrawable implementation
        // -----------------------------------------------------------------

        /// <summary>
        /// Called by the MAUI graphics pipeline every time <see cref="Invalidate"/>
        /// is raised. Renders one radial circle per heat map point.
        /// </summary>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var points = HeatMapPoints;
            if (points is null || points.Count == 0)
                return;

            double latRange  = NorthWestLatitude  - SouthEastLatitude;
            double lonRange  = SouthEastLongitude - NorthWestLongitude;

            // Guard: skip if the viewport bounds haven't been set yet.
            if (Math.Abs(latRange) < 1e-9 || Math.Abs(lonRange) < 1e-9)
                return;

            foreach (var point in points)
            {
                // Convert GPS coordinates to canvas pixel coordinates.
                float x = (float)((point.Longitude - NorthWestLongitude) / lonRange * dirtyRect.Width);
                float y = (float)((NorthWestLatitude - point.Latitude)   / latRange * dirtyRect.Height);

                // Skip points that lie outside the current viewport.
                if (x < -200 || x > dirtyRect.Width + 200 ||
                    y < -200 || y > dirtyRect.Height + 200)
                    continue;

                // Radius in pixels – scaled by a rough pixels-per-degree estimate.
                float pixelsPerDegree = (float)(dirtyRect.Width / lonRange);
                float radiusDegrees   = (float)(HeatMapService.IntensityToRadius(point.NormalisedIntensity) / 111_320.0);
                float radius          = radiusDegrees * pixelsPerDegree;

                DrawHeatCircle(canvas, x, y, radius, point.NormalisedIntensity);
            }
        }

        // -----------------------------------------------------------------
        // Private drawing helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Paints a single heat circle using a series of filled ellipses
        /// with decreasing opacity to simulate a radial gradient.
        /// (MAUI's ICanvas does not support radial gradients directly.)
        /// </summary>
        private static void DrawHeatCircle(ICanvas canvas, float cx, float cy,
                                            float radius, double intensity)
        {
            // Number of concentric rings. More rings = smoother gradient but slower draw.
            const int Rings = 8;

            var coreColor = HeatMapService.IntensityToColor(intensity);

            for (int i = Rings; i >= 1; i--)
            {
                float  fraction = (float)i / Rings;
                float  r        = radius * fraction;
                float  alpha    = (float)(0.08 * (1.0 - fraction) + 0.03);

                canvas.FillColor = new Color(
                    coreColor.Red,
                    coreColor.Green,
                    coreColor.Blue,
                    alpha);

                canvas.FillEllipse(cx - r, cy - r, r * 2, r * 2);
            }

            // Draw a small opaque dot at the exact sample location.
            float dotRadius = Math.Max(3f, radius * 0.08f);
            canvas.FillColor = new Color(
                coreColor.Red, coreColor.Green, coreColor.Blue, 0.85f);
            canvas.FillEllipse(cx - dotRadius, cy - dotRadius, dotRadius * 2, dotRadius * 2);
        }

        // -----------------------------------------------------------------
        // Property-changed callbacks
        // -----------------------------------------------------------------

        private static void OnHeatMapPointsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var overlay = (HeatMapOverlay)bindable;

            // Unsubscribe from the old collection to prevent memory leaks.
            if (oldValue is ObservableCollection<HeatMapPoint> oldCollection)
                oldCollection.CollectionChanged -= overlay.OnPointsCollectionChanged;

            // Subscribe to the new collection so the canvas redraws on mutations.
            if (newValue is ObservableCollection<HeatMapPoint> newCollection)
                newCollection.CollectionChanged += overlay.OnPointsCollectionChanged;

            overlay.Invalidate();
        }

        private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            Invalidate();

        private static void OnViewportChanged(BindableObject bindable, object _, object __)
        {
            ((HeatMapOverlay)bindable).Invalidate();
        }
    }
}
