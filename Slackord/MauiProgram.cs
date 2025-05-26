using CommunityToolkit.Maui;

namespace Slackord
{
    public static class MauiProgram
    {
        /// <summary>
        /// Configures and builds the MAUI application
        /// </summary>
        /// <returns>The configured MauiApp instance</returns>
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            _ = builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    _ = fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    _ = fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            return builder.Build();
        }
    }
}
