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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IAuthStateService, AuthStateService>();

builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7119/";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient<DashboardApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7119/";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7119/";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(DashboardApiClient).Assembly);

app.Run();
