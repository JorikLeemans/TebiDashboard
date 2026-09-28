using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// SuiteQL paging should refresh an expired bearer token instead of failing the whole sync.
/// </summary>
public sealed class NetSuiteRestClientTests
{
    [Fact]
    public async Task QuerySuiteQlAsync_RetriesPage_AfterUnauthorized()
    {
        var tokens = new FakeTokenProvider(["expired", "fresh"]);
        var handler = new SequenceHandler(
        [
            (HttpStatusCode.Unauthorized, """{"title":"Unauthorized"}"""),
            (HttpStatusCode.OK, """{"items":[{"id":"1","name":"Bill"}],"hasMore":false}""")
        ]);
        var client = CreateClient(tokens, handler);

        var rows = await client.QuerySuiteQlAsync("SELECT 1 FROM vendorBill", pageSize: 100);

        Assert.Single(rows);
        Assert.Equal("1", rows[0]["id"].GetString());
        Assert.Equal(1, tokens.Invalidations);
        Assert.Equal(2, handler.CallCount);
        Assert.Equal("Bearer fresh", handler.AuthorizationHeaders[1]);
    }

    [Fact]
    public async Task QuerySuiteQlAsync_RequestsTokenForEachPage()
    {
        var tokens = new FakeTokenProvider(["a", "b"]);
        var handler = new SequenceHandler(
        [
            (HttpStatusCode.OK, """{"items":[{"id":"1"}],"hasMore":true}"""),
            (HttpStatusCode.OK, """{"items":[{"id":"2"}],"hasMore":false}""")
        ]);
        var client = CreateClient(tokens, handler);

        var rows = await client.QuerySuiteQlAsync("SELECT 1 FROM vendorBill", pageSize: 1);

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, tokens.TokensIssued);
    }

    private static NetSuiteRestClient CreateClient(INetSuiteTokenProvider tokens, HttpMessageHandler handler)
    {
        var settings = new NetSuiteOptions
        {
            Enabled = true,
            AccountId = "1234567",
            ClientId = "client",
            CertificateId = "kid",
            PrivateKeyPem = "-----BEGIN PRIVATE KEY-----\nTEST\n-----END PRIVATE KEY-----"
        };

        return new NetSuiteRestClient(
            tokens,
            Options.Create(settings),
            new StaticHttpClientFactory(handler),
            NullLogger<NetSuiteRestClient>.Instance);
    }

    private sealed class FakeTokenProvider(IReadOnlyList<string> tokens) : INetSuiteTokenProvider
    {
        private int _index;

        public int Invalidations { get; private set; }

        public int TokensIssued { get; private set; }

        public bool IsConfigured => true;

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            TokensIssued++;
            var token = tokens[Math.Min(_index, tokens.Count - 1)];
            _index++;
            return Task.FromResult(token);
        }

        public void InvalidateCachedToken() => Invalidations++;
    }

    private sealed class StaticHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        private readonly HttpClient _client = new(handler);

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses;

        public SequenceHandler(IEnumerable<(HttpStatusCode Status, string Body)> responses)
        {
            _responses = new Queue<(HttpStatusCode, string)>(responses);
        }

        public int CallCount { get; private set; }

        public List<string> AuthorizationHeaders { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString() ?? string.Empty);
            var (status, body) = _responses.Dequeue();
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
