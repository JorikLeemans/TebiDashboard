namespace TebiDashboard.Maui.Services;

/// <summary>
/// Camera barcode scan in the native app (Android and Windows). The phone website uses the browser camera.
/// </summary>
public interface IBarcodeScannerService
{
    Task<string?> ScanAsync(CancellationToken cancellationToken = default);
}
