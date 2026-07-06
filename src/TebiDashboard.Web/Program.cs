using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using TebiDashboard.UI.Services;
using TebiDashboard.Web.Components;

// Default to Dutch locale so currency and dates render with € instead of ¤.
var dutchCulture = CultureInfo.GetCultureInfo("nl-NL");
CultureInfo.DefaultThreadCurrentCulture = dutchCulture;
CultureInfo.DefaultThreadCurrentUICulture = dutchCulture;

var builder = WebApplication.CreateBuilder(args);

// Railway and other PaaS hosts assign a dynamic PORT at runtime.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Trust reverse-proxy headers from Railway (required for HTTPS + antiforgery behind the edge).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Persist antiforgery keys in the container so Blazor interactive circuits stay stable across restarts.
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IAuthStateService, AuthStateService>();
builder.Services.AddScoped<IPeriodFilterService, PeriodFilterService>();

var apiBaseUrl = ResolveApiBaseUrl(builder.Configuration["ApiBaseUrl"]);
var syncTimeoutMinutes = builder.Configuration.GetValue("NetSuite:SyncHttpTimeoutMinutes", 30);

builder.Services.AddHttpClient<AuthApiClient>(client => client.BaseAddress = apiBaseUrl);
builder.Services.AddHttpClient<DashboardApiClient>(client => client.BaseAddress = apiBaseUrl);
builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    client.BaseAddress = apiBaseUrl;
    // NetSuite sync can run many SuiteQL pages; default HttpClient timeout is only 100 seconds.
    client.Timeout = TimeSpan.FromMinutes(Math.Clamp(syncTimeoutMinutes, 1, 120));
});

var app = builder.Build();

// Forwarded headers must run before HTTPS redirects, antiforgery, and Blazor endpoints.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Railway terminates TLS at the edge; redirecting to HTTPS inside the container causes request failures.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.MapStaticAssets();

// Lightweight health probe for Railway (no Blazor circuit required).
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "TebiDashboard.Web",
    apiBaseUrl = apiBaseUrl.ToString()
}));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(DashboardApiClient).Assembly);

app.Logger.LogInformation("TebiDashboard.Web started. ApiBaseUrl={ApiBaseUrl}", apiBaseUrl);

app.Run();

// Accepts host-only Railway values like "Tebi-api-production.up.railway.app" and normalizes to https://…/
static Uri ResolveApiBaseUrl(string? configuredUrl)
{
    var value = (configuredUrl ?? "https://localhost:7119/").Trim();

    if (!value.Contains("://", StringComparison.Ordinal))
    {
        value = $"https://{value}";
    }

    if (!value.EndsWith('/'))
    {
        value += "/";
    }

    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException($"ApiBaseUrl is not a valid absolute URL: '{configuredUrl}'");
    }

    return uri;
}
