using LocationTracker.Services;
using LocationTracker.ViewModels;
using LocationTracker.Views;
using Microsoft.Extensions.Logging;

namespace LocationTracker;

/// <summary>
/// Application entry point for all platforms. Configures the MAUI app,
/// enables the Maps component and registers services for dependency injection.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Builds and configures the <see cref="MauiApp"/>.
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            // Required to use the Microsoft.Maui.Controls.Maps.Map control (and our HeatMap).
            .UseMauiMaps();

        RegisterServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Registers services, view models and views with the DI container.
    /// </summary>
    private static void RegisterServices(IServiceCollection services)
    {
        // Platform APIs (exposed as interfaces so they can be mocked in tests).
        services.AddSingleton(Geolocation.Default);

        // Data + tracking services are singletons: one database connection
        // and one tracking session for the lifetime of the app.
        services.AddSingleton<ILocationRepository, LocationRepository>();
        services.AddSingleton<ILocationTrackingService, LocationTrackingService>();

        // UI
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainView>();
    }
}
