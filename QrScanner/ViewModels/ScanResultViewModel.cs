using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public sealed partial class ScanResultViewModel : ViewModelBase, IDisposable
{
    private readonly ParsedQrContent? _parsed;
    private readonly Action? _onDismiss;

    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Bitmap? Image { get; }
    public string? RawText { get; }
    public string DisplayText { get; }
    public ContentKind Kind { get; }
    public string? ActionLabel => _parsed?.ActionLabel;
    public string? ActionUri => _parsed?.ActionUri;
    public bool CanOpenAction => _parsed?.ActionUri is not null;
    public bool IsWifi => _parsed?.Wifi is not null;
    public string? WifiSsid => _parsed?.Wifi?.Ssid;
    public string? ImagePath { get; }
    public bool IsWifiConnectSupported { get; } = PlatformServices.WifiConnectorFactory is not null;
    public bool IsShareSupported => PlatformServices.ShareFactory is not null && !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath);

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public string Title => IsSuccess ? "Scan result" : "Scan failed";

    public string KindBadge => Kind switch
    {
        ContentKind.Url => "Website",
        ContentKind.WiFi => "Wi-Fi Network",
        ContentKind.Email => "Email Address",
        ContentKind.Phone => "Phone Number",
        ContentKind.VCard => "Contact Card",
        _ => "Text"
    };

    private ScanResultViewModel(
        bool isSuccess,
        string? errorMessage,
        Bitmap? image,
        string? rawText,
        ParsedQrContent? parsed,
        string? imagePath,
        Action? onDismiss)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Image = image;
        RawText = rawText;
        _parsed = parsed;
        DisplayText = parsed?.DisplayText ?? rawText ?? string.Empty;
        Kind = parsed?.Kind ?? ContentKind.Text;
        ImagePath = imagePath;
        _onDismiss = onDismiss;
    }

    public static ScanResultViewModel CreateSuccess(
        string rawText,
        byte[] jpegBytes,
        string imagePath,
        Action? onDismiss)
    {
        var parsed = QrContentParser.Parse(rawText);
        var bitmap = new Bitmap(new MemoryStream(jpegBytes));
        return new ScanResultViewModel(
            isSuccess: true,
            errorMessage: null,
            image: bitmap,
            rawText: rawText,
            parsed: parsed,
            imagePath: imagePath,
            onDismiss: onDismiss);
    }

    public static ScanResultViewModel CreateFailure(
        byte[]? imageBytes,
        string errorMessage,
        Action? onDismiss)
    {
        Bitmap? bitmap = null;
        if (imageBytes is { Length: > 0 })
        {
            try
            {
                bitmap = new Bitmap(new MemoryStream(imageBytes));
            }
            catch
            {
                // Ignore image decode errors
            }
        }

        return new ScanResultViewModel(
            isSuccess: false,
            errorMessage: errorMessage,
            image: bitmap,
            rawText: null,
            parsed: null,
            imagePath: null,
            onDismiss: onDismiss);
    }

    [RelayCommand]
    private void Dismiss() => _onDismiss?.Invoke();

    [RelayCommand]
    private async Task CopyTextAsync(TopLevel? topLevel)
    {
        if (!string.IsNullOrEmpty(RawText) && topLevel?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(RawText).ConfigureAwait(true);
            StatusMessage = "Copied to clipboard!";
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
            return;

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

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
        {
            var share = PlatformServices.ShareFactory?.Invoke();
            if (share is not null)
                await share.ShareImageAsync(ImagePath).ConfigureAwait(true);
        }
    }

    public void Dispose()
    {
        Image?.Dispose();
    }
}
