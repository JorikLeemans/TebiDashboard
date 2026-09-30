using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using TebiDashboard.Api.Authorization;
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

var signingKey = AuthSigningKey.Resolve(builder.Configuration, builder.Environment);
var issuer = builder.Configuration["Auth:Issuer"] ?? "TebiDashboard";
var audience = builder.Configuration["Auth:Audience"] ?? "TebiDashboard";
var lifetimeHours = Math.Clamp(builder.Configuration.GetValue("Auth:TokenLifetimeHours", 12), 1, 24);
builder.Services.AddSingleton(new JwtAccessTokenFactory(
    signingKey,
    issuer,
    audience,
    TimeSpan.FromHours(lifetimeHours)));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the short "role" and "sub" claim names from the token.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            RoleClaimType = "role",
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Every endpoint requires a signed-in user unless it is marked AllowAnonymous.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Clients", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();

        // An empty list trusts no browser origin. Credentials are only sent to the configured hosts.
        if (allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy.WithOrigins(allowedOrigins).AllowCredentials();
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
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTimeouts();
app.MapControllers();

app.Run();

// Expose Program for WebApplicationFactory in integration tests.
public partial class Program;
