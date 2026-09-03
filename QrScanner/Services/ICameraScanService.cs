using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace QrScanner.Services;

public sealed class QrDetectedEventArgs : EventArgs
{
    public required string RawText { get; init; }

    /// <summary>A JPEG snapshot of the frame the code was detected in, saved into scan history.</summary>
    public required byte[] JpegImage { get; init; }
}

/// <summary>
/// Platform-specific camera capture + QR decoding. Implementations own a native camera preview
/// control and raise <see cref="QrDetected"/> whenever a QR code is found in a captured frame.
/// </summary>
public interface ICameraScanService : IDisposable
{
    event EventHandler<QrDetectedEventArgs>? QrDetected;

    /// <summary>Creates the (platform-native) preview control. Call once and host it in the view tree.</summary>
    Control CreatePreviewControl();

    Task<bool> RequestPermissionAsync();

    Task StartAsync();

    Task StopAsync();
}
