using TebiDashboard.Application.Options;

namespace TebiDashboard.Application.Tests.NetSuite;

/// <summary>
/// Unit tests for NetSuite configuration validation helpers.
/// </summary>
public sealed class NetSuiteOptionsTests
{
    [Fact]
    public void GetMissingConfigurationKeys_ListsAllRequiredFields()
    {
        var options = new NetSuiteOptions();

        var missing = options.GetMissingConfigurationKeys();

        Assert.Contains("Enabled", missing);
        Assert.Contains("AccountId", missing);
        Assert.Contains("ClientId", missing);
        Assert.Contains("CertificateId", missing);
        Assert.Contains("PrivateKeyPem or PrivateKeyPath", missing);
    }

    [Fact]
    public void IsConfigured_IsTrue_WhenInlinePemIsSet()
    {
        var options = new NetSuiteOptions
        {
            Enabled = true,
            AccountId = "9156349_SB1",
            ClientId = "client",
            CertificateId = "cert",
            PrivateKeyPem = "-----BEGIN PRIVATE KEY-----\nTEST\n-----END PRIVATE KEY-----"
        };

        Assert.True(options.IsConfigured);
        Assert.Empty(options.GetMissingConfigurationKeys());
    }
}
