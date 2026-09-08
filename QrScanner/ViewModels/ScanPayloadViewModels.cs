using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public abstract partial class ScanPayloadViewModel : ViewModelBase
{
    private readonly Action? _onDismiss;

    protected ScanPayloadViewModel(string rawText, ParsedQrContent content, string? imagePath, Action? onDismiss)
    {
        RawText = rawText;
        Content = content;
        ImagePath = imagePath;
        _onDismiss = onDismiss;
    }

    public ParsedQrContent Content { get; }
    public string RawText { get; }
    public string DisplayText => Content.DisplayText;
    public ContentKind Kind => Content.Kind;
    public string? ActionUri => Content.ActionUri;
    public string? ActionLabel => Content.ActionLabel;
    public string? ImagePath { get; }
    public bool IsShareSupported => PlatformServices.ShareFactory is not null && !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath);

    protected virtual string CopyStatusMessage => "Copied to clipboard.";

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [RelayCommand]
    private async Task CopyTextAsync(TopLevel? topLevel)
    {
        if (topLevel?.Clipboard is null)
        {
            StatusMessage = "Clipboard isn't available.";
            return;
        }

        await topLevel.Clipboard.SetTextAsync(RawText).ConfigureAwait(true);
        StatusMessage = CopyStatusMessage;
    }

    [RelayCommand]
    private async Task OpenActionAsync(TopLevel? topLevel)
    {
        if (ActionUri is not null && topLevel?.Launcher is not null)
        {
            await topLevel.Launcher.LaunchUriAsync(new Uri(ActionUri)).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (!IsShareSupported || ImagePath is null)
        {
            return;
        }

        var share = PlatformServices.ShareFactory?.Invoke();
        if (share is not null)
        {
            await share.ShareImageAsync(ImagePath).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private void Dismiss() => _onDismiss?.Invoke();
}

public sealed partial class TextPayloadViewModel(string rawText, ParsedQrContent content, string? imagePath, Action? onDismiss)
    : ScanPayloadViewModel(rawText, content, imagePath, onDismiss);

public sealed partial class UrlPayloadViewModel(string rawText, ParsedQrContent content, string? imagePath, Action? onDismiss)
    : ScanPayloadViewModel(rawText, content, imagePath, onDismiss)
{
    protected override string CopyStatusMessage => "Link copied.";
}

public sealed partial class EmailPayloadViewModel(string rawText, ParsedQrContent content, string? imagePath, Action? onDismiss)
    : ScanPayloadViewModel(rawText, content, imagePath, onDismiss)
{
    protected override string CopyStatusMessage => "Email action copied.";
}

public sealed partial class PhonePayloadViewModel(string rawText, ParsedQrContent content, string? imagePath, Action? onDismiss)
    : ScanPayloadViewModel(rawText, content, imagePath, onDismiss)
{
    protected override string CopyStatusMessage => "Phone action copied.";
}

public sealed partial class VCardPayloadViewModel(string rawText, ParsedQrContent content, string? imagePath, Action? onDismiss)
    : ScanPayloadViewModel(rawText, content, imagePath, onDismiss)
{
    public bool IsContactImportSupported { get; } = PlatformServices.ContactImporterFactory is not null;

    protected override string CopyStatusMessage => "Contact card copied.";

    [RelayCommand]
    private async Task ImportContactAsync()
    {
        var importer = PlatformServices.ContactImporterFactory?.Invoke();
        if (importer is null)
        {
            StatusMessage = "Contact import isn't supported on this platform.";
            return;
        }

        StatusMessage = await importer.ImportVCardAsync(RawText).ConfigureAwait(true)
            ? "Opened contact import."
            : "Couldn't open contact import.";
    }
}

public sealed partial class WifiPayloadViewModel(string rawText, ParsedQrContent content, string? imagePath, Action? onDismiss)
    : ScanPayloadViewModel(rawText, content, imagePath, onDismiss)
{
    public bool IsWifiConnectSupported { get; } = PlatformServices.WifiConnectorFactory is not null;
    public string WifiSsid => Content.Wifi?.Ssid ?? "Unknown network";
    public string WifiSecurity => Content.Wifi?.Security switch
    {
        Models.WifiSecurity.Wep => "WEP",
        Models.WifiSecurity.Wpa => "WPA/WPA2/WPA3",
        Models.WifiSecurity.None => "Open",
        _ => "Unknown"
    };
    public string WifiVisibility => Content.Wifi?.Hidden is true ? "Hidden" : "Visible";

    protected override string CopyStatusMessage => "Wi-Fi details copied.";

    [RelayCommand]
    private async Task ConnectWifiAsync()
    {
        if (Content.Wifi is not { } wifi)
        {
            StatusMessage = "Wi-Fi details are incomplete.";
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
}

public static class ScanPayloadViewModelFactory
{
    public static ScanPayloadViewModel Create(string rawText, ParsedQrContent content, string? imagePath, Action? onDismiss) =>
        content.Kind switch
        {
            ContentKind.Url => new UrlPayloadViewModel(rawText, content, imagePath, onDismiss),
            ContentKind.Email => new EmailPayloadViewModel(rawText, content, imagePath, onDismiss),
            ContentKind.Phone => new PhonePayloadViewModel(rawText, content, imagePath, onDismiss),
            ContentKind.WiFi => new WifiPayloadViewModel(rawText, content, imagePath, onDismiss),
            ContentKind.VCard => new VCardPayloadViewModel(rawText, content, imagePath, onDismiss),
            _ => new TextPayloadViewModel(rawText, content, imagePath, onDismiss)
        };
}
