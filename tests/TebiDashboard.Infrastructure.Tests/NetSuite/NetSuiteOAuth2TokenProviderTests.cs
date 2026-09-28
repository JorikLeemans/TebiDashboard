using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Token refresh must sign a second client assertion without disposing the RSA key.
/// </summary>
public sealed class NetSuiteOAuth2TokenProviderTests
{
    [Fact]
    public async Task GetAccessTokenAsync_CanSignASecondJwt_AfterCacheInvalidation()
    {
        var handler = new StubHandler();
        using var provider = CreateProvider(handler);

        var first = await provider.GetAccessTokenAsync();
        provider.InvalidateCachedToken();
        var second = await provider.GetAccessTokenAsync();

        Assert.Equal("token-1", first);
        Assert.Equal("token-2", second);
        Assert.Equal(2, handler.ClientAssertions.Count);
        Assert.All(handler.ClientAssertions, assertion => Assert.False(string.IsNullOrWhiteSpace(assertion)));
        Assert.NotEqual(handler.ClientAssertions[0], handler.ClientAssertions[1]);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReusesCachedToken_UntilInvalidated()
    {
        var handler = new StubHandler();
        using var provider = CreateProvider(handler);

        var first = await provider.GetAccessTokenAsync();
        var second = await provider.GetAccessTokenAsync();

        Assert.Equal(first, second);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public void CacheLifetime_RefreshesTwoMinutesEarly()
    {
        Assert.Equal(TimeSpan.FromSeconds(3480), NetSuiteOAuth2TokenProvider.CacheLifetime(3600));
        Assert.Equal(TimeSpan.FromSeconds(60), NetSuiteOAuth2TokenProvider.CacheLifetime(30));
        Assert.Equal(TimeSpan.FromSeconds(3480), NetSuiteOAuth2TokenProvider.CacheLifetime(0));
    }

    private static NetSuiteOAuth2TokenProvider CreateProvider(StubHandler handler)
    {
        using var rsa = RSA.Create(2048);
        var settings = new NetSuiteOptions
        {
            Enabled = true,
            AccountId = "1234567_SB1",
            ClientId = "client-id",
            CertificateId = "cert-kid",
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
            Scopes = ["rest_webservices"]
        };

        return new NetSuiteOAuth2TokenProvider(
            Options.Create(settings),
            new MemoryCache(new MemoryCacheOptions()),
            new StaticHttpClientFactory(handler),
            NullLogger<NetSuiteOAuth2TokenProvider>.Instance);
    }

    private sealed class StaticHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        private readonly HttpClient _client = new(handler);

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public List<string> ClientAssertions { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var form = await request.Content!.ReadAsStringAsync(cancellationToken);
            ClientAssertions.Add(Uri.UnescapeDataString(ExtractAssertion(form)));

            var body = $$"""{"access_token":"token-{{CallCount}}","token_type":"bearer","expires_in":3600}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private static string ExtractAssertion(string form)
        {
            foreach (var pair in form.Split('&'))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 && parts[0] == "client_assertion")
                {
                    return parts[1];
                }
            }

            return string.Empty;
        }
    }
}
