using TebiDashboard.Application.Options;
using TebiDashboard.Infrastructure.NetSuite;

namespace TebiDashboard.Infrastructure.Tests.NetSuite;

/// <summary>
/// Unit tests for NetSuite private key resolution from PEM text or file path.
/// </summary>
public sealed class NetSuitePrivateKeyResolverTests
{
    [Fact]
    public void ResolvePem_PrefersInlinePem_OverFilePath()
    {
        var settings = new NetSuiteOptions
        {
            PrivateKeyPem = "-----BEGIN PRIVATE KEY-----\\nABC\\n-----END PRIVATE KEY-----",
            PrivateKeyPath = "missing.pem"
        };

        var pem = NetSuitePrivateKeyResolver.ResolvePem(settings);

        Assert.Contains("-----BEGIN PRIVATE KEY-----", pem, StringComparison.Ordinal);
        Assert.Contains('\n', pem);
    }

    [Fact]
    public void ResolvePem_ReadsFromFile_WhenPemNotSet()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"netsuite-test-{Guid.NewGuid():N}.pem");
        File.WriteAllText(tempFile, "-----BEGIN PRIVATE KEY-----\nTEST\n-----END PRIVATE KEY-----");

        try
        {
            var settings = new NetSuiteOptions { PrivateKeyPath = tempFile };
            var pem = NetSuitePrivateKeyResolver.ResolvePem(settings);
            Assert.Contains("TEST", pem, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
