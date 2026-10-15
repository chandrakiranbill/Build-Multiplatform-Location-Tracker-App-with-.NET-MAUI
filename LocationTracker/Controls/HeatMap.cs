using System.Collections.Specialized;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
// Aliases avoid clashes with Microsoft.Maui.ApplicationModel.Map and Shapes.Circle.
using Circle = Microsoft.Maui.Controls.Maps.Circle;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace LocationTracker.Controls;

/// <summary>
/// Extends the .NET MAUI <see cref="Map"/> control with heat map rendering.
/// Bind <see cref="Points"/> to a collection of locations; the control groups them
/// into a grid and draws one coloured <see cref="Circle"/> per cell, coloured from
/// blue (rarely visited) to red (frequently visited).
/// </summary>
public class HeatMap : Map
{
    /// <summary>Bindable property for <see cref="Points"/>.</summary>
    public static readonly BindableProperty PointsProperty = BindableProperty.Create(
        nameof(Points),
        typeof(IEnumerable<Location>),
        typeof(HeatMap),
        defaultValue: null,
        propertyChanged: OnPointsChanged);

    /// <summary>Bindable property for <see cref="CellSizeMeters"/>.</summary>
    public static readonly BindableProperty CellSizeMetersProperty = BindableProperty.Create(
        nameof(CellSizeMeters),
        typeof(double),
        typeof(HeatMap),
        defaultValue: 150d,
        propertyChanged: (bindable, _, _) => ((HeatMap)bindable).Redraw());

    /// <summary>Bindable property for <see cref="FollowLatestPoint"/>.</summary>
    public static readonly BindableProperty FollowLatestPointProperty = BindableProperty.Create(
        nameof(FollowLatestPoint),
        typeof(bool),
        typeof(HeatMap),
        defaultValue: true);

    /// <summary>Radius of the visible region when the map centres on the data.</summary>
    private static readonly Distance DefaultVisibleRadius = Distance.FromKilometers(3);

    private bool _hasCenteredOnData;

    public HeatMap()
    {
        MapType = MapType.Street;
    }

    /// <summary>
    /// Gets or sets the locations to visualise. If the collection implements
    /// <see cref="INotifyCollectionChanged"/> (e.g. ObservableCollection) the map
    /// redraws automatically when items are added or removed.
    /// </summary>
    public IEnumerable<Location>? Points
    {
        get => (IEnumerable<Location>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    /// <summary>
    /// Gets or sets the size of a heat map grid cell (and the radius of each circle) in metres.
    /// </summary>
    public double CellSizeMeters
    {
        get => (double)GetValue(CellSizeMetersProperty);
        set => SetValue(CellSizeMetersProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the map should pan to each newly added point.
    /// </summary>
    public bool FollowLatestPoint
    {
        get => (bool)GetValue(FollowLatestPointProperty);
        set => SetValue(FollowLatestPointProperty, value);
    }

    /// <summary>
    /// Re-subscribes to collection change notifications when the bound collection changes.
    /// </summary>
    private static void OnPointsChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var map = (HeatMap)bindable;

        if (oldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= map.OnCollectionChanged;
        }

        if (newValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += map.OnCollectionChanged;
        }

        map._hasCenteredOnData = false;
        map.Redraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _hasCenteredOnData = false;
        }

        Redraw();

        // Follow the user as new positions arrive.
        if (FollowLatestPoint &&
            e.Action == NotifyCollectionChangedAction.Add &&
            e.NewItems is { Count: > 0 } newItems &&
            newItems[newItems.Count - 1] is Location latest)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                MoveToRegion(MapSpan.FromCenterAndRadius(latest, DefaultVisibleRadius)));
        }
    }

    /// <summary>
    /// Rebuilds all heat map circles from the current <see cref="Points"/>.
    /// Map elements must be changed on the UI thread.
    /// </summary>
    private void Redraw()
    {
        // Take a snapshot so the collection can keep changing while we draw.
        var snapshot = Points?.ToList() ?? [];

        MainThread.BeginInvokeOnMainThread(() =>
        {
            MapElements.Clear();

            var cells = HeatMapCalculator.Calculate(snapshot, CellSizeMeters);
            foreach (var cell in cells)
            {
                MapElements.Add(CreateCircle(cell));
            }

            CenterOnDataOnce(snapshot);
        });
    }

    /// <summary>
    /// Creates the visual circle representing one heat map cell.
    /// </summary>
    private Circle CreateCircle(HeatCell cell)
    {
        var color = HeatMapCalculator.GetHeatColor(cell.Intensity);

        return new Circle
        {
            Center = cell.Center,
            // Hot cells are drawn slightly larger so they stand out.
            Radius = Distance.FromMeters(CellSizeMeters * (0.6 + (0.4 * cell.Intensity))),
            FillColor = color,
            StrokeColor = color.WithAlpha(0.9f),
            StrokeWidth = 2,
        };
    }

    /// <summary>
    /// The first time data is available, zoom the map so the recorded track is visible.
    /// </summary>
    private void CenterOnDataOnce(IReadOnlyList<Location> points)
    {
        if (_hasCenteredOnData || points.Count == 0)
        {
            return;
        }

        var minLat = points.Min(p => p.Latitude);
        var maxLat = points.Max(p => p.Latitude);
        var minLon = points.Min(p => p.Longitude);
        var maxLon = points.Max(p => p.Longitude);

        var center = new Location((minLat + maxLat) / 2, (minLon + maxLon) / 2);

        // Add 20% padding around the data, with a sensible minimum zoom.
        var latDegrees = Math.Max((maxLat - minLat) * 1.2, 0.02);
        var lonDegrees = Math.Max((maxLon - minLon) * 1.2, 0.02);

        MoveToRegion(new MapSpan(center, latDegrees, lonDegrees));
        _hasCenteredOnData = true;
    }
}
