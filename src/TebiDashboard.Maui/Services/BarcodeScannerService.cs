namespace TebiDashboard.Maui.Services;

/// <summary>
/// Stub barcode scanner until ZXing UI page is wired; returns demo barcode for tests.
/// </summary>
public sealed class BarcodeScannerService : IBarcodeScannerService
{
    public Task<string?> ScanAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<string?>("8712345000011");
}
