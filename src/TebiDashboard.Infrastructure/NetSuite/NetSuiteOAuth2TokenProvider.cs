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
    ILogger<NetSuiteOAuth2TokenProvider> logger) : INetSuiteTokenProvider, IDisposable
{
    private const string TokenCacheKey = "netsuite-oauth2-access-token";
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly object _rsaLock = new();
    private RSA? _signingRsa;

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "NetSuite is not configured. Set AccountId, ClientId, CertificateId, PrivateKeyPem or PrivateKeyPath, and Enabled=true.");
        }

        if (TryGetCachedToken(out var cachedToken))
        {
            return cachedToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCachedToken(out cachedToken))
            {
                return cachedToken;
            }

            var (token, expiresIn) = await RequestAccessTokenAsync(settings, cancellationToken);
            memoryCache.Set(TokenCacheKey, token, CacheLifetime(expiresIn));
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public void InvalidateCachedToken() => memoryCache.Remove(TokenCacheKey);

    private bool TryGetCachedToken(out string token)
    {
        if (memoryCache.TryGetValue(TokenCacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            token = cached;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private async Task<(string AccessToken, int ExpiresIn)> RequestAccessTokenAsync(
        NetSuiteOptions settings,
        CancellationToken cancellationToken)
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

        return (tokenResponse.AccessToken, tokenResponse.ExpiresIn);
    }

    private string CreateClientAssertion(NetSuiteOptions settings, string tokenEndpoint)
    {
        var rsa = GetSigningRsa(settings);
        var now = DateTimeOffset.UtcNow;
        // KeyId becomes the JWT header "kid" required by NetSuite M2M certificate mapping.
        var signingKey = new RsaSecurityKey(rsa) { KeyId = settings.CertificateId };
        // IdentityModel caches signature providers by kid. Disposing the RSA after the first JWT
        // (as `using var rsa` did) made the next token refresh throw ObjectDisposedException on RSAOpenSsl.
        signingKey.CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false };
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

    /// <summary>
    /// Loads the M2M private key once and keeps RSA alive for later token refreshes during long syncs.
    /// </summary>
    private RSA GetSigningRsa(NetSuiteOptions settings)
    {
        if (_signingRsa is not null)
        {
            return _signingRsa;
        }

        lock (_rsaLock)
        {
            if (_signingRsa is not null)
            {
                return _signingRsa;
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(NetSuitePrivateKeyResolver.ResolvePem(settings));
            _signingRsa = rsa;
            return _signingRsa;
        }
    }

    /// <summary>Caches the bearer token until two minutes before NetSuite expires it.</summary>
    internal static TimeSpan CacheLifetime(int expiresInSeconds)
    {
        var seconds = expiresInSeconds > 0 ? expiresInSeconds : 3600;
        return TimeSpan.FromSeconds(Math.Clamp(seconds - 120, 60, 3600));
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

    public void Dispose()
    {
        _tokenLock.Dispose();
        _signingRsa?.Dispose();
    }

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
