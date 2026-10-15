using Foundation;

namespace LocationTracker;

/// <summary>
/// Mac Catalyst application delegate that boots the MAUI app.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
