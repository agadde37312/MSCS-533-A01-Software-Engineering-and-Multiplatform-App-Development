// MauiProgram.cs
// Configures the MAUI application host, registers all services and pages in
// the Dependency Injection container, and enables platform features such as
// Maps and Geolocation.

using CommunityToolkit.Maui;
using LocationHeatMap.Services;
using LocationHeatMap.ViewModels;
using LocationHeatMap.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;

namespace LocationHeatMap
{
    /// <summary>
    /// Static factory that builds and returns the <see cref="MauiApp"/>.
    /// Called by the platform-specific entry points (e.g., MainActivity on Android).
    /// </summary>
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                // Enable the CommunityToolkit.Maui helpers.
                .UseMauiCommunityToolkit()
                // Enable the MAUI Maps component (requires a platform API key
                // configured in each platform's manifest / Info.plist).
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf",    "OpenSansRegular");
                    fonts.AddFont("OpenSans-SemiBold.ttf",   "OpenSansSemiBold");
                });

#if DEBUG
            // Enable verbose MAUI logging in debug builds only.
            // AddDebug() requires Microsoft.Extensions.Logging.Debug package;
            // use the built-in MAUI debug logger instead to avoid the dependency.
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

            // ── Register Services (Singletons survive the app lifetime) ──────
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<HeatMapService>();

            // ── Register ViewModel ───────────────────────────────────────────
            // Singleton because we only ever have one instance of MainPage.
            builder.Services.AddSingleton<MainViewModel>();

            // ── Register Pages ───────────────────────────────────────────────
            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }
}
