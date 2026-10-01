using TebiDashboard.UI.Services;

namespace TebiDashboard.Maui.Services;

/// <summary>
/// Opens the device camera and returns the barcode it reads.
/// </summary>
public sealed class BarcodeScannerService : IBarcodeScannerService, INativeBarcodeScanner
{
    public async Task<string?> ScanAsync(CancellationToken cancellationToken = default)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            return null;
        }

        var page = new BarcodeScanPage();
        var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
            ?? throw new InvalidOperationException("The app has no page to show the camera on.");

        await MainThread.InvokeOnMainThreadAsync(() => navigation.PushModalAsync(page));
        return await page.Result.WaitAsync(cancellationToken);
    }
}
