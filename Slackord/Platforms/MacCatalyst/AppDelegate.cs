using Foundation;

namespace Slackord
{
    /// <summary>
    /// Mac Catalyst application delegate that serves as the entry point for the MAUI application on macOS.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <summary>
        /// Creates and returns the MAUI application instance.
        /// </summary>
        /// <returns>The configured MauiApp instance.</returns>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
