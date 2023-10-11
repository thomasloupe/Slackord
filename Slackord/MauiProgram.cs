using CommunityToolkit.Maui;

namespace Slackord
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            _ = builder
                .UseMauiApp<App>()
                // Initialize the .NET MAUI Community Toolkit by adding the below line of code
                .UseMauiCommunityToolkit()
                // After initializing the .NET MAUI Community Toolkit, optionally add additional fonts
                .ConfigureFonts(fonts =>
                {
                    _ = fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    _ = fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Continue initializing your .NET MAUI App here

            return builder.Build();
        }
    }
}
