using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace TebiDashboard.Maui;

/// <summary>
/// Full-screen camera used by the Android and Windows app. The first barcode closes the page.
/// </summary>
public sealed class BarcodeScanPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _result = new();
    private bool _completed;

    public BarcodeScanPage()
    {
        Title = "Scan barcode";
        BackgroundColor = Colors.Black;

        var camera = new CameraBarcodeReaderView
        {
            CameraLocation = CameraLocation.Rear,
            IsDetecting = true,
            Options = new BarcodeReaderOptions
            {
                // EAN, UPC and Code 128 are the codes printed on TEBI articles.
                Formats = BarcodeFormats.OneDimensional,
                AutoRotate = true,
                Multiple = false
            }
        };
        camera.BarcodesDetected += (_, args) =>
        {
            var value = args.Results?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            camera.IsDetecting = false;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                Complete(value.Trim());
                if (Navigation.ModalStack.Count > 0)
                {
                    await Navigation.PopModalAsync();
                }
            });
        };

        var hint = new Label
        {
            Text = "Point the camera at the barcode",
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 8)
        };
        var cancel = new Button
        {
            Text = "Cancel",
            Margin = new Thickness(16),
            BackgroundColor = Color.FromArgb("#F4C430"),
            TextColor = Color.FromArgb("#1A3D2B")
        };
        cancel.Clicked += async (_, _) =>
        {
            Complete(null);
            if (Navigation.ModalStack.Count > 0)
            {
                await Navigation.PopModalAsync();
            }
        };

        var layout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            }
        };
        layout.Add(camera);
        Grid.SetRow(hint, 1);
        layout.Add(hint);
        Grid.SetRow(cancel, 2);
        layout.Add(cancel);
        Content = layout;
    }

    public Task<string?> Result => _result.Task;

    protected override void OnDisappearing()
    {
        // Back closes the camera without a code. A detected barcode is already stored.
        Complete(null);
        base.OnDisappearing();
    }

    private void Complete(string? value)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _result.TrySetResult(value);
    }
}
