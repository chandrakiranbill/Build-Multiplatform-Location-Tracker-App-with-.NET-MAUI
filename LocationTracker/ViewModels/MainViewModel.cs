using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocationTracker.Models;
using LocationTracker.Services;

namespace LocationTracker.ViewModels;

/// <summary>
/// View model for <see cref="Views.MainView"/>. Loads saved locations from SQLite,
/// starts/stops tracking and exposes the points the heat map displays.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILocationRepository _repository;
    private readonly ILocationTrackingService _trackingService;

    private bool _isLoaded;

    /// <summary>Whether GPS tracking is currently running.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackingButtonText))]
    private bool _isTracking;

    /// <summary>Whether a long-running operation is in progress.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Short status message shown under the map.</summary>
    [ObservableProperty]
    private string _statusText = "Loading saved locations…";

    public MainViewModel(ILocationRepository repository, ILocationTrackingService trackingService)
    {
        _repository = repository;
        _trackingService = trackingService;

        _trackingService.LocationSaved += OnLocationSaved;
        _trackingService.TrackingFailed += OnTrackingFailed;
    }

    /// <summary>
    /// All recorded locations. Bound to <c>HeatMap.Points</c>; the map redraws when it changes.
    /// </summary>
    public ObservableCollection<Location> Points { get; } = [];

    /// <summary>Label for the start/stop button.</summary>
    public string TrackingButtonText => IsTracking ? "Stop tracking" : "Start tracking";

    /// <summary>
    /// Loads saved points and starts tracking. Called when the page appears.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;

        await ReloadPointsAsync();
        await StartTrackingAsync();
    }

    /// <summary>Toggles tracking on or off.</summary>
    [RelayCommand]
    private async Task ToggleTrackingAsync()
    {
        if (IsTracking)
        {
            _trackingService.Stop();
            IsTracking = false;
            UpdateStatus("Tracking paused");
        }
        else
        {
            await StartTrackingAsync();
        }
    }

    /// <summary>
    /// Inserts a sample route into the database so the heat map can be demonstrated
    /// on an emulator/simulator without physically moving.
    /// </summary>
    [RelayCommand]
    private async Task SimulateRouteAsync()
    {
        IsBusy = true;
        try
        {
            // Start the demo route at the latest known point, or at the device's last
            // known position, or fall back to Mountain View, CA (emulator default).
            var start = Points.LastOrDefault()
                        ?? await Geolocation.Default.GetLastKnownLocationAsync()
                        ?? new Location(37.3230, -122.0322);

            var route = SampleRouteGenerator.Create(start);
            await _repository.SaveAllAsync(route);
            await ReloadPointsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Deletes all stored locations after confirmation.</summary>
    [RelayCommand]
    private async Task ClearAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        var confirmed = page is null || await page.DisplayAlert(
            "Clear history", "Delete all recorded locations?", "Delete", "Cancel");

        if (!confirmed)
        {
            return;
        }

        await _repository.ClearAsync();
        Points.Clear();
        UpdateStatus("History cleared");
    }

    private async Task StartTrackingAsync()
    {
        try
        {
            IsTracking = await _trackingService.StartAsync();
            UpdateStatus(IsTracking ? "Tracking your location" : "Location permission denied");
        }
        catch (FeatureNotSupportedException)
        {
            UpdateStatus("GPS is not supported on this device");
        }
        catch (FeatureNotEnabledException)
        {
            UpdateStatus("Please turn on location services");
        }
        catch (PermissionException)
        {
            UpdateStatus("Location permission denied");
        }
    }

    /// <summary>Re-reads all points from SQLite into <see cref="Points"/>.</summary>
    private async Task ReloadPointsAsync()
    {
        var saved = await _repository.GetAllAsync();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Points.Clear();
            foreach (var point in saved)
            {
                Points.Add(point.ToLocation());
            }
        });

        UpdateStatus(IsTracking ? "Tracking your location" : "Ready");
    }

    /// <summary>
    /// Called by the tracking service (background thread) after a point is saved.
    /// ObservableCollection must be modified on the UI thread.
    /// </summary>
    private void OnLocationSaved(object? sender, LocationPoint point)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Points.Add(point.ToLocation());
            UpdateStatus("Tracking your location");
        });
    }

    private void OnTrackingFailed(object? sender, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsTracking = false;
            UpdateStatus(message);
        });
    }

    private void UpdateStatus(string message) =>
        StatusText = $"{message} • {Points.Count} point(s) saved";
}
