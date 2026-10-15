namespace LocationTracker;

/// <summary>
/// The application class. Creates the main window hosting the <see cref="AppShell"/>.
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
