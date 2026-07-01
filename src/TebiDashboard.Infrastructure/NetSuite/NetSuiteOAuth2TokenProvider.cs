using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Requests NetSuite OAuth 2.0 access tokens using the client credentials (M2M) JWT flow.
/// </summary>
public sealed class NetSuiteOAuth2TokenProvider(
    IOptions<NetSuiteOptions> options,
    IMemoryCache memoryCache,
    IHttpClientFactory httpClientFactory,
    ILogger<NetSuiteOAuth2TokenProvider> logger) : INetSuiteTokenProvider
{
    private const string TokenCacheKey = "netsuite-oauth2-access-token";
    private static readonly TimeSpan TokenCacheDuration = TimeSpan.FromMinutes(55);

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "NetSuite is not configured. Set NetSuite:AccountId, ClientId, CertificateId, PrivateKeyPath, and Enabled=true.");
        }

        if (memoryCache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
        {
            return cachedToken;
        }

        var token = await RequestAccessTokenAsync(settings, cancellationToken);
        memoryCache.Set(TokenCacheKey, token, TokenCacheDuration);
        return token;
    }

    private async Task<string> RequestAccessTokenAsync(NetSuiteOptions settings, CancellationToken cancellationToken)
    {
        var tokenEndpoint = BuildTokenEndpoint(settings.AccountId);
        var assertion = CreateClientAssertion(settings, tokenEndpoint);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
            ["client_assertion"] = assertion
        });

        var client = httpClientFactory.CreateClient(NetSuiteHttpClientNames.Api);
        using var response = await client.PostAsync(tokenEndpoint, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("NetSuite token request failed with status {StatusCode}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"NetSuite token request failed ({(int)response.StatusCode}): {body}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<NetSuiteTokenResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
        {
            throw new InvalidOperationException("NetSuite token response did not include access_token.");
        }

        return tokenResponse.AccessToken;
    }

    private static string CreateClientAssertion(NetSuiteOptions settings, string tokenEndpoint)
    {
        var privateKeyPem = File.ReadAllText(ResolvePrivateKeyPath(settings.PrivateKeyPath));
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        var now = DateTimeOffset.UtcNow;
        // KeyId becomes the JWT header "kid" required by NetSuite M2M certificate mapping.
        var signingKey = new RsaSecurityKey(rsa) { KeyId = settings.CertificateId };
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSsaPssSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.ClientId,
            Audience = tokenEndpoint,
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(5).UtcDateTime,
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>
            {
                // NetSuite expects the requested scopes in the client assertion payload.
                ["scope"] = settings.Scopes
            }
        };

        var handler = new JsonWebTokenHandler();
        var assertion = handler.CreateToken(descriptor);
        if (string.IsNullOrWhiteSpace(assertion))
        {
            throw new InvalidOperationException("Failed to create NetSuite client assertion JWT.");
        }

        return assertion;
    }

    private static string ResolvePrivateKeyPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }

    internal static string BuildTokenEndpoint(string accountId) =>
        $"https://{NormalizeAccountIdForUrl(accountId)}.suitetalk.api.netsuite.com/services/rest/auth/oauth2/v1/token";

    internal static string BuildRestBaseUrl(string accountId) =>
        $"https://{NormalizeAccountIdForUrl(accountId)}.suitetalk.api.netsuite.com/services/rest";

    /// <summary>
    /// NetSuite account IDs use underscores in the UI (e.g. 9156349_SB1) but hyphens in REST hostnames (9156349-sb1).
    /// </summary>
    internal static string NormalizeAccountIdForUrl(string accountId) =>
        accountId.Replace('_', '-').ToLowerInvariant();

    private sealed class NetSuiteTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}

/// <summary>
/// Named HttpClient identifiers for NetSuite HTTP traffic.
/// </summary>
public static class NetSuiteHttpClientNames
{
    public const string Api = "NetSuiteApi";
}
