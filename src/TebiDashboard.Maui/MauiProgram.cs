using System.Globalization;
using Microsoft.Extensions.Logging;
using TebiDashboard.Maui.Services;
using TebiDashboard.UI.Services;
using ZXing.Net.Maui.Controls;

namespace TebiDashboard.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Default to Dutch locale so currency and dates render with € instead of ¤.
        var dutchCulture = CultureInfo.GetCultureInfo("nl-NL");
        CultureInfo.DefaultThreadCurrentCulture = dutchCulture;
        CultureInfo.DefaultThreadCurrentUICulture = dutchCulture;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<BarcodeScannerService>();
        builder.Services.AddSingleton<IBarcodeScannerService>(services => services.GetRequiredService<BarcodeScannerService>());
        builder.Services.AddSingleton<INativeBarcodeScanner>(services => services.GetRequiredService<BarcodeScannerService>());
        builder.Services.AddScoped<IAuthStateService, AuthStateService>();
        builder.Services.AddScoped<IPeriodFilterService, PeriodFilterService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var apiBaseUrl = "https://10.0.2.2:7119/"; // Android emulator → host machine API
#if WINDOWS
        apiBaseUrl = "https://localhost:7119/";
#endif

        builder.Services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl),
            // NetSuite sync can exceed the default 100 second HttpClient timeout.
            Timeout = TimeSpan.FromMinutes(30)
        });
        builder.Services.AddSingleton<AuthApiClient>();
        builder.Services.AddScoped<DashboardApiClient>();
        builder.Services.AddScoped<AdminApiClient>();

        return builder.Build();
    }
}
