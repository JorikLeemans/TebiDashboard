using TebiDashboard.Application.Options;

namespace TebiDashboard.Infrastructure.NetSuite;

/// <summary>
/// Loads the NetSuite M2M private key from inline PEM text or a file path.
/// </summary>
internal static class NetSuitePrivateKeyResolver
{
    public static string ResolvePem(NetSuiteOptions settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PrivateKeyPem))
        {
            // Hosted env vars often store PEM newlines as literal \n sequences.
            return settings.PrivateKeyPem.Replace("\\n", "\n", StringComparison.Ordinal).Trim();
        }

        if (string.IsNullOrWhiteSpace(settings.PrivateKeyPath))
        {
            throw new InvalidOperationException(
                "NetSuite private key is missing. Set NetSuite:PrivateKeyPem or NetSuite:PrivateKeyPath.");
        }

        var path = ResolvePrivateKeyPath(settings.PrivateKeyPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"NetSuite private key file was not found at '{path}'.", path);
        }

        return File.ReadAllText(path).Trim();
    }

    internal static string ResolvePrivateKeyPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }
}
