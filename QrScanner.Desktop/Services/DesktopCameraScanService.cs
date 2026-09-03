using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using FlashCap;
using QrScanner.Services;
using SkiaImageView;
using SkiaSharp;
using FlashCapPixelFormats = FlashCap.PixelFormats;

namespace QrScanner.Desktop.Services;

/// <summary>
/// Desktop (dev-convenience) camera capture using FlashCap for frame grabbing, the shared
/// ZXing/SkiaSharp decoder, and SkiaImageView (FlashCap's own recommended companion control) to
/// render frames without any manual pixel/stride handling.
/// </summary>
public sealed class DesktopCameraScanService : ICameraScanService
{
    // The color pipeline for this capture path (FlashCap raw frame -> SkiaSharp) has proven
    // unreliable across cameras (wrong tint/range); rendering grayscale sidesteps that entirely
    // and QR decoding only ever needed luminance anyway.
    private static readonly SKColorFilter GrayscaleFilter = SKColorFilter.CreateColorMatrix(
    [
        0.299f, 0.587f, 0.114f, 0, 0,
        0.299f, 0.587f, 0.114f, 0, 0,
        0.299f, 0.587f, 0.114f, 0, 0,
        0,      0,      0,      1, 0
    ]);

    private readonly SKImageView _previewView = new()
    {
        Stretch = Avalonia.Media.Stretch.Uniform,
        // This camera delivers frames upside-down; flip only the displayed preview so the
        // underlying bitmap used for decoding/history (already scanning correctly) stays untouched.
        RenderTransform = new ScaleTransform(1, -1),
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
    };

    private CaptureDevice? _device;
    private DateTime _lastDecodeAttemptUtc = DateTime.MinValue;

    public event EventHandler<QrDetectedEventArgs>? QrDetected;

    public Control CreatePreviewControl() => _previewView;

    public Task<bool> RequestPermissionAsync() => Task.FromResult(true);

    public async Task StartAsync()
    {
        if (_device is not null)
        {
            return;
        }

        var devices = new CaptureDevices();
        var descriptor = devices.EnumerateDescriptors().FirstOrDefault();
        if (descriptor is null)
        {
            return;
        }

        var characteristics = descriptor.Characteristics.FirstOrDefault(c => c.PixelFormat != FlashCapPixelFormats.Unknown);
        if (characteristics is null)
        {
            return;
        }

        _device = await descriptor.OpenAsync(characteristics, OnFrameArrivedAsync).ConfigureAwait(true);
        await _device.StartAsync().ConfigureAwait(true);
    }

    public async Task StopAsync()
    {
        if (_device is null)
        {
            return;
        }

        await _device.StopAsync().ConfigureAwait(true);
        _device.Dispose();
        _device = null;
    }

    private async Task OnFrameArrivedAsync(PixelBufferScope bufferScope)
    {
        // CopyImage() makes a safe, fully-owned copy; ReferImage() is zero-copy but the buffer can
        // be reused/overwritten by the next frame concurrently with decoding, causing tearing.
        var imageBytes = bufferScope.Buffer.CopyImage();
        using var stream = new MemoryStream(imageBytes, writable: false);
        using var bitmap = SKBitmap.Decode(stream);

        if (bitmap is null)
        {
            return;
        }

        // SKImageView takes ownership of the bitmap (and disposes the previous one) once assigned.
        var previewBitmap = ApplyGrayscale(bitmap, flipVertical: false);
        await Dispatcher.UIThread.InvokeAsync(() => _previewView.Source = previewBitmap);

        // Throttle decoding attempts; QR decoding is comparatively expensive.
        var now = DateTime.UtcNow;
        if (now - _lastDecodeAttemptUtc < TimeSpan.FromMilliseconds(250))
        {
            return;
        }
        _lastDecodeAttemptUtc = now;

        using var decodeFrame = Downscale(bitmap, maxDimension: 640);
        var text = QrDecoder.TryDecode(decodeFrame);
        if (text is null)
        {
            return;
        }

        using var upright = ApplyGrayscale(bitmap, flipVertical: true);
        using var jpeg = upright.Encode(SKEncodedImageFormat.Jpeg, 85);
        QrDetected?.Invoke(this, new QrDetectedEventArgs { RawText = text, JpegImage = jpeg.ToArray() });
    }

    /// <summary>Returns a smaller copy for fast QR decoding; returns the original if it's already small enough.</summary>
    private static SKBitmap Downscale(SKBitmap bitmap, int maxDimension)
    {
        var largestSide = Math.Max(bitmap.Width, bitmap.Height);
        if (largestSide <= maxDimension)
        {
            return bitmap.Copy();
        }

        var scale = maxDimension / (float)largestSide;
        var info = new SKImageInfo((int)(bitmap.Width * scale), (int)(bitmap.Height * scale));
        return bitmap.Resize(info, new SKSamplingOptions(SKFilterMode.Linear)) ?? bitmap.Copy();
    }

    /// <summary>Converts to grayscale, optionally flipping vertically to undo this camera's upside-down frames.</summary>
    private static SKBitmap ApplyGrayscale(SKBitmap bitmap, bool flipVertical)
    {
        var result = new SKBitmap(bitmap.Width, bitmap.Height);
        using var canvas = new SKCanvas(result);
        using var paint = new SKPaint { ColorFilter = GrayscaleFilter };

        if (flipVertical)
        {
            canvas.Scale(1, -1, 0, bitmap.Height / 2f);
        }

        canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(), paint);
        return result;
    }

    public void Dispose()
    {
        _device?.Dispose();
    }
}


