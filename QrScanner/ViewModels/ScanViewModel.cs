using System;
using System.IO;
using System.Threading;
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
    private readonly Action<ScanRecord, byte[]> _onScanCompleted;
    private readonly SemaphoreSlim _cameraGate = new(1, 1);
    private ICameraScanService? _camera;

    private string? _lastRawText;
    private DateTime _lastDetectedAtUtc;
    private volatile bool _shouldRunCamera;
    private bool _isCameraRunning;

    [ObservableProperty]
    public partial Control? PreviewControl { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Point the camera at a QR code";

    public ScanViewModel(IDatabaseService db, Action<ScanRecord, byte[]> onScanCompleted)
    {
        _db = db;
        _onScanCompleted = onScanCompleted;
        EnsureCamera();
    }

    public async Task StartAsync()
    {
        EnsureCamera();

        if (_camera is null)
        {
            return;
        }

        _shouldRunCamera = true;
        await _cameraGate.WaitAsync().ConfigureAwait(true);

        try
        {
            if (!_shouldRunCamera || _isCameraRunning)
            {
                return;
            }

            if (!await _camera.RequestPermissionAsync().ConfigureAwait(true))
            {
                StatusMessage = "Camera permission was denied.";
                _shouldRunCamera = false;
                return;
            }

            if (!_shouldRunCamera)
                return;

            StatusMessage = "Point the camera at a QR code";
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_shouldRunCamera)
                {
                    PreviewControl = null;
                    PreviewControl = _camera.CreatePreviewControl();
                }
            });

            if (!_shouldRunCamera)
            {
                await Dispatcher.UIThread.InvokeAsync(() => PreviewControl = null);
                return;
            }

            await _camera.StartAsync().ConfigureAwait(true);

            if (!_shouldRunCamera)
            {
                await _camera.StopAsync().ConfigureAwait(true);
                await Dispatcher.UIThread.InvokeAsync(() => PreviewControl = null);
                return;
            }

            _isCameraRunning = true;
        }
        catch
        {
            _isCameraRunning = false;
            if (_shouldRunCamera)
            {
                StatusMessage = "Failed to start camera.";
            }
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PreviewControl = null;
            });
        }
        finally
        {
            _cameraGate.Release();
        }
    }

    public async Task StopAsync()
    {
        _shouldRunCamera = false;

        await _cameraGate.WaitAsync().ConfigureAwait(true);

        try
        {
            if (_shouldRunCamera)
            {
                return;
            }

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

            _isCameraRunning = false;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_shouldRunCamera)
                {
                    PreviewControl = null;
                }
            });
        }
        finally
        {
            _cameraGate.Release();
        }
    }

    public void PrepareForExternalImageIntent()
    {
        _shouldRunCamera = false;
        _isCameraRunning = false;
        ClearPreviewControl();

        _ = StopAsync();
    }

    private void ClearPreviewControl()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            PreviewControl = null;
        }
        else
        {
            Dispatcher.UIThread.Post(() => PreviewControl = null);
        }
    }

    private void EnsureCamera()
    {
        if (_camera is not null)
        {
            return;
        }

        _camera = PlatformServices.CameraFactory?.Invoke();
        if (_camera is not null)
        {
            _camera.QrDetected += OnQrDetected;
        }
    }

    private async void OnQrDetected(object? sender, QrDetectedEventArgs e)
    {
        if (!_shouldRunCamera)
        {
            return;
        }

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
        _cameraGate.Dispose();
    }
}
