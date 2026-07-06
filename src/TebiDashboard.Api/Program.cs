using Microsoft.AspNetCore.HttpOverrides;
using TebiDashboard.Application;
using TebiDashboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Railway and other PaaS hosts assign a dynamic PORT at runtime.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("NetSuiteSync", TimeSpan.FromMinutes(
        Math.Clamp(builder.Configuration.GetValue("NetSuite:SyncHttpTimeoutMinutes", 30), 1, 120)));
});
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Clients", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials();
    });
});

// Trust reverse-proxy headers from Railway (required when TLS terminates at the edge).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Forwarded headers must run before other middleware.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.UseCors("Clients");
app.UseRequestTimeouts();
app.MapControllers();

app.Run();

// Expose Program for WebApplicationFactory in integration tests.
public partial class Program;
