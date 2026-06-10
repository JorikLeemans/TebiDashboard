namespace TebbiDashboard.Maui.Services;

/// <summary>
/// Abstraction for platform barcode scanning (Android camera / Windows scanner).
/// </summary>
public interface IBarcodeScannerService
{
    Task<string?> ScanAsync(CancellationToken cancellationToken = default);
}
