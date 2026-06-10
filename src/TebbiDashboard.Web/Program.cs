using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using TebbiDashboard.UI.Services;
using TebbiDashboard.Web.Components;

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

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7119/";
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri))
{
    throw new InvalidOperationException($"ApiBaseUrl is not a valid absolute URL: '{apiBaseUrl}'");
}

builder.Services.AddHttpClient<AuthApiClient>(client => client.BaseAddress = apiUri);
builder.Services.AddHttpClient<DashboardApiClient>(client => client.BaseAddress = apiUri);
builder.Services.AddHttpClient<AdminApiClient>(client => client.BaseAddress = apiUri);

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
    service = "TebbiDashboard.Web",
    apiBaseUrl = apiBaseUrl
}));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(DashboardApiClient).Assembly);

app.Logger.LogInformation("TebbiDashboard.Web started. ApiBaseUrl={ApiBaseUrl}", apiBaseUrl);

app.Run();
