using Microsoft.Extensions.Logging;
using TebiDashboard.Maui.Services;
using TebiDashboard.UI.Services;
using ZXing.Net.Maui.Controls;

namespace TebiDashboard.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<IBarcodeScannerService, BarcodeScannerService>();
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

        builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
        builder.Services.AddSingleton<AuthApiClient>();
        builder.Services.AddSingleton<DashboardApiClient>();
        builder.Services.AddScoped<AdminApiClient>();

        return builder.Build();
    }
}
