using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public sealed partial class ScanViewModel : ViewModelBase, IDisposable
{
    private readonly IDatabaseService _db;
    private readonly ICameraScanService? _camera;
    private readonly Action<ScanRecord, byte[]> _onScanCompleted;

    private string? _lastRawText;
    private DateTime _lastDetectedAtUtc;

    [ObservableProperty]
    public partial Control? PreviewControl { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Point the camera at a QR code";

    private bool _isCameraRunning;

    public ScanViewModel(IDatabaseService db, Action<ScanRecord, byte[]> onScanCompleted)
    {
        _db = db;
        _onScanCompleted = onScanCompleted;
        _camera = PlatformServices.CameraFactory?.Invoke();

        if (_camera is not null)
        {
            _camera.QrDetected += OnQrDetected;
        }
    }

    public async Task StartAsync()
    {
        if (_camera is null || _isCameraRunning)
        {
            return;
        }

        _isCameraRunning = true;

        try
        {
            if (!await _camera.RequestPermissionAsync().ConfigureAwait(true))
            {
                StatusMessage = "Camera permission was denied.";
                _isCameraRunning = false;
                return;
            }

            if (!_isCameraRunning)
                return;

            StatusMessage = "Point the camera at a QR code";
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_isCameraRunning)
                    PreviewControl = _camera.CreatePreviewControl();
            });

            if (!_isCameraRunning)
            {
                await Dispatcher.UIThread.InvokeAsync(() => PreviewControl = null);
                return;
            }

            await _camera.StartAsync().ConfigureAwait(true);

            if (!_isCameraRunning)
            {
                await _camera.StopAsync().ConfigureAwait(true);
                await Dispatcher.UIThread.InvokeAsync(() => PreviewControl = null);
            }
        }
        catch
        {
            _isCameraRunning = false;
            StatusMessage = "Failed to start camera.";
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PreviewControl = null;
            });
        }
    }

    public async Task StopAsync()
    {
        _isCameraRunning = false;

        if (_camera is not null)
        {
            try
            {
                await _camera.StopAsync().ConfigureAwait(true);
            }
            catch
            {
                // Ignore stop errors
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            PreviewControl = null;
        });
    }

    private async void OnQrDetected(object? sender, QrDetectedEventArgs e)
    {
        if (e.RawText == _lastRawText && DateTime.UtcNow - _lastDetectedAtUtc < TimeSpan.FromSeconds(3))
        {
            return;
        }

        _lastRawText = e.RawText;
        _lastDetectedAtUtc = DateTime.UtcNow;
        var parsed = QrContentParser.Parse(e.RawText);

        var fileName = $"{Guid.NewGuid():N}.jpg";
        var path = Path.Combine(AppPaths.ImagesDirectory, fileName);
        await File.WriteAllBytesAsync(path, e.JpegImage).ConfigureAwait(false);

        var record = new ScanRecord
        {
            ScannedAtUtc = DateTime.UtcNow,
            RawText = e.RawText,
            Kind = parsed.Kind,
            ImageFileName = fileName
        };
        await _db.InsertAsync(record).ConfigureAwait(false);

        await StopAsync().ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _onScanCompleted(record, e.JpegImage);
        });
    }

    public void Dispose()
    {
        if (_camera is not null)
        {
            _camera.QrDetected -= OnQrDetected;
            _camera.Dispose();
        }
    }
}
