using UIKit;

namespace LocationTracker;

/// <summary>
/// iOS process entry point.
/// </summary>
public static class Program
{
    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
