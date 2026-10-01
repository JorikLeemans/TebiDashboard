namespace TebiDashboard.UI.Services;

/// <summary>
/// Camera barcode scan provided by the native app. The website leaves this unregistered
/// and uses the browser camera instead.
/// </summary>
public interface INativeBarcodeScanner
{
    /// <summary>Opens the camera and returns the barcode, or null when the user cancels.</summary>
    Task<string?> ScanAsync(CancellationToken cancellationToken = default);
}
