using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public partial class ScanViewModel : ViewModelBase, IDisposable
{
    private readonly IDatabaseService _db;
    private readonly ICameraScanService? _camera;

    private string? _lastRawText;
    private DateTime _lastDetectedAtUtc;
    private ParsedQrContent? _parsed;

    [ObservableProperty]
    public partial Control? PreviewControl { get; set; }

    [ObservableProperty]
    public partial bool HasResult { get; set; }

    [ObservableProperty]
    public partial string ResultText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ActionLabel { get; set; }

    [ObservableProperty]
    public partial bool CanOpenAction { get; set; }

    [ObservableProperty]
    public partial bool IsWifiResult { get; set; }

    [ObservableProperty]
    public partial string? WifiSsid { get; set; }

    public bool IsWifiConnectSupported { get; } = PlatformServices.WifiConnectorFactory is not null;

    [ObservableProperty]
    public partial Bitmap? ResultImage { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Point the camera at a QR code";

    public ScanViewModel(IDatabaseService db)
    {
        _db = db;
        _camera = PlatformServices.CameraFactory?.Invoke();

        if (_camera is not null)
        {
            PreviewControl = _camera.CreatePreviewControl();
            _camera.QrDetected += OnQrDetected;
        }
    }

    public async Task StartAsync()
    {
        if (_camera is null)
        {
            StatusMessage = "Camera is not available on this platform.";
            return;
        }

        if (!await _camera.RequestPermissionAsync().ConfigureAwait(true))
        {
            StatusMessage = "Camera permission was denied.";
            return;
        }

        StatusMessage = "Point the camera at a QR code";
        await _camera.StartAsync().ConfigureAwait(true);
    }

    public async Task StopAsync()
    {
        if (_camera is not null)
        {
            await _camera.StopAsync().ConfigureAwait(true);
        }
    }

    private async void OnQrDetected(object? sender, QrDetectedEventArgs e)
    {
        // Ignore repeats of the same code within a short window so a code doesn't get re-saved every frame.
        if (e.RawText == _lastRawText && DateTime.UtcNow - _lastDetectedAtUtc < TimeSpan.FromSeconds(3))
        {
            return;
        }

        _lastRawText = e.RawText;
        _lastDetectedAtUtc = DateTime.UtcNow;
        _parsed = QrContentParser.Parse(e.RawText);

        var fileName = $"{Guid.NewGuid():N}.jpg";
        var path = Path.Combine(AppPaths.ImagesDirectory, fileName);
        await File.WriteAllBytesAsync(path, e.JpegImage).ConfigureAwait(false);

        var record = new ScanRecord
        {
            ScannedAtUtc = DateTime.UtcNow,
            RawText = e.RawText,
            Kind = _parsed.Kind,
            ImageFileName = fileName
        };
        await _db.InsertAsync(record).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ResultText = _parsed.DisplayText;
            ActionLabel = _parsed.ActionLabel;
            CanOpenAction = _parsed.ActionUri is not null;
            IsWifiResult = _parsed.Wifi is not null;
            WifiSsid = _parsed.Wifi?.Ssid;
            HasResult = true;
            StatusMessage = "Scanned!";
            ResultImage = new Bitmap(new MemoryStream(e.JpegImage));
        });
    }

    [RelayCommand]
    private async Task CopyTextAsync(TopLevel? topLevel)
    {
        if (topLevel?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(ResultText).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task OpenLinkAsync(TopLevel? topLevel)
    {
        if (_parsed?.ActionUri is not null && topLevel?.Launcher is not null)
        {
            await topLevel.Launcher.LaunchUriAsync(new Uri(_parsed.ActionUri)).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task ConnectWifiAsync()
    {
        if (_parsed?.Wifi is not { } wifi)
        {
            return;
        }

        var connector = PlatformServices.WifiConnectorFactory?.Invoke();
        if (connector is null)
        {
            StatusMessage = "Wi-Fi auto-connect isn't supported on this platform.";
            return;
        }

        StatusMessage = await connector.ConnectAsync(wifi).ConfigureAwait(true)
            ? "Requested Wi-Fi connection."
            : "Couldn't start the Wi-Fi connection.";
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
