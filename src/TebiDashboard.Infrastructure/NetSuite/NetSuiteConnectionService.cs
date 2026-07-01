using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TebiDashboard.Application.Interfaces;
using TebiDashboard.Application.Options;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Tests OAuth 2.0 connectivity against the NetSuite REST metadata endpoint.
/// </summary>
public sealed class NetSuiteConnectionService(
    IOptions<NetSuiteOptions> options,
    INetSuiteTokenProvider tokenProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<NetSuiteConnectionService> logger) : INetSuiteConnectionService
{
    public async Task<NetSuiteConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            return new NetSuiteConnectionResult(
                IsConfigured: false,
                IsConnected: false,
                Message: settings.DescribeMissingConfiguration()
                    + " Set NetSuite environment variables on the API service (e.g. NetSuite__Enabled=true).");
        }

        string? tokenPreview = null;
        try
        {
            var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            tokenPreview = accessToken.Length <= 16
                ? accessToken
                : $"{accessToken[..8]}...{accessToken[^8..]}";

            // metadata-catalog can be huge; use a small record query as a REST smoke test instead.
            var smokeTestUrl = $"{NetSuiteOAuth2TokenProvider.BuildRestBaseUrl(settings.AccountId)}/record/v1/customer?limit=1";
            using var smokeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            smokeCts.CancelAfter(TimeSpan.FromSeconds(30));

            var client = httpClientFactory.CreateClient(NetSuiteHttpClientNames.Api);
            using var request = new HttpRequestMessage(HttpMethod.Get, smokeTestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(request, smokeCts.Token);
            var body = await response.Content.ReadAsStringAsync(smokeCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("NetSuite REST smoke test failed with status {StatusCode}: {Body}", response.StatusCode, body);
                return new NetSuiteConnectionResult(
                    IsConfigured: true,
                    IsConnected: true,
                    Message: "Access token obtained. REST smoke test returned a non-success status; verify role permissions for REST Web Services.",
                    TokenPreview: tokenPreview);
            }

            return new NetSuiteConnectionResult(
                IsConfigured: true,
                IsConnected: true,
                Message: "Successfully obtained an access token and reached the NetSuite REST API.",
                TokenPreview: tokenPreview);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Token worked but the optional REST call was too slow; still report connectivity.
            logger.LogWarning("NetSuite REST smoke test timed out after obtaining a token.");
            return new NetSuiteConnectionResult(
                IsConfigured: true,
                IsConnected: true,
                Message: "Access token obtained. REST smoke test timed out, but OAuth credentials are valid.",
                TokenPreview: tokenPreview);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "NetSuite connection test failed.");
            return new NetSuiteConnectionResult(
                IsConfigured: true,
                IsConnected: false,
                Message: ex.Message);
        }
    }
}
