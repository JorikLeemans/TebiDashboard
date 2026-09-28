using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Executes authenticated SuiteQL queries against the NetSuite REST API.
/// </summary>
public sealed class NetSuiteRestClient(
    INetSuiteTokenProvider tokenProvider,
    IOptions<NetSuiteOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<NetSuiteRestClient> logger) : INetSuiteRestClient
{
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, JsonElement>>> QuerySuiteQlAsync(
        string sql,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("NetSuite is not configured for SuiteQL queries.");
        }

        var baseUrl = NetSuiteOAuth2TokenProvider.BuildRestBaseUrl(settings.AccountId);
        var client = httpClientFactory.CreateClient(NetSuiteHttpClientNames.Api);
        var results = new List<IReadOnlyDictionary<string, JsonElement>>();
        var offset = 0;
        var limit = Math.Clamp(pageSize, 1, 1000);
        var retriedUnauthorized = false;

        while (true)
        {
            // Re-read the cached token each page so a long vendor-bill import can refresh after ~1 hour.
            var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            var url = $"{baseUrl}/query/v1/suiteql?limit={limit}&offset={offset}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Prefer", "transient");
            request.Content = JsonContent.Create(new SuiteQlRequest(sql));

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && !retriedUnauthorized)
            {
                logger.LogWarning("SuiteQL returned 401; refreshing the NetSuite access token and retrying the page.");
                tokenProvider.InvalidateCachedToken();
                retriedUnauthorized = true;
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SuiteQL request failed with status {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"SuiteQL request failed ({(int)response.StatusCode}): {body}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                    foreach (var property in item.EnumerateObject())
                    {
                        if (property.NameEquals("links"))
                        {
                            continue;
                        }

                        row[property.Name] = property.Value.Clone();
                    }

                    if (row.Count > 0)
                    {
                        results.Add(row);
                    }
                }
            }

            var hasMore = root.TryGetProperty("hasMore", out var hasMoreElement) && hasMoreElement.GetBoolean();
            if (!hasMore)
            {
                break;
            }

            offset += limit;
            retriedUnauthorized = false;
        }

        return results;
    }

    private sealed class SuiteQlRequest
    {
        public SuiteQlRequest(string query) => Q = query;

        public string Q { get; }
    }
}
