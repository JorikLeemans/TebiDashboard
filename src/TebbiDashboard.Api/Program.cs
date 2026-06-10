using TebbiDashboard.Application;
using TebbiDashboard.Infrastructure;
using TebbiDashboard.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

var app = builder.Build();

// Apply migrations and seed demo data when a persistent provider is configured.
await app.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Clients");
app.MapControllers();

app.Run();

// Expose Program for WebApplicationFactory in integration tests.
public partial class Program;
