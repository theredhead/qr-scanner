using System;
using System.IO;
using System.Threading.Tasks;
using ScannerZero.Models;
using ScannerZero.Services;
using ScannerZero.ViewModels;
using Xunit;

namespace ScannerZero.Tests;

public sealed class ScanPayloadViewModelFactoryTests : IDisposable
{
    public ScanPayloadViewModelFactoryTests()
    {
        PlatformServices.ContactImporterFactory = null;
        PlatformServices.ShareFactory = null;
        PlatformServices.WifiConnectorFactory = null;
    }

    [Theory]
    [InlineData(ContentKind.Text, typeof(TextPayloadViewModel))]
    [InlineData(ContentKind.Url, typeof(UrlPayloadViewModel))]
    [InlineData(ContentKind.Email, typeof(EmailPayloadViewModel))]
    [InlineData(ContentKind.Phone, typeof(PhonePayloadViewModel))]
    [InlineData(ContentKind.VCard, typeof(VCardPayloadViewModel))]
    public void Create_returns_specific_view_model_for_payload_kind(ContentKind kind, Type expectedType)
    {
        var payload = new ParsedScanPayload(kind, "ScannerZero", "https://github.com/theredhead/qr-scanner", "Open");

        var viewModel = ScanPayloadViewModelFactory.Create("ScannerZero", payload, null, null);

        Assert.IsType(expectedType, viewModel);
        Assert.Equal(kind, viewModel.Kind);
        Assert.Equal("ScannerZero", viewModel.RawText);
        Assert.Equal(payload.ActionUri, viewModel.ActionUri);
        Assert.Equal(payload.ActionLabel, viewModel.ActionLabel);
    }

    [Fact]
    public void Create_returns_wifi_view_model_with_display_properties()
    {
        var payload = new ParsedScanPayload(
            ContentKind.WiFi,
            "ScannerZero Wi-Fi",
            null,
            null,
            new WifiCredentials("ScannerZero", "3.14159", WifiSecurity.Wpa, true));

        var viewModel = Assert.IsType<WifiPayloadViewModel>(
            ScanPayloadViewModelFactory.Create("raw", payload, null, null));

        Assert.Equal("ScannerZero", viewModel.WifiSsid);
        Assert.Equal("WPA/WPA2/WPA3", viewModel.WifiSecurity);
        Assert.Equal("Hidden", viewModel.WifiVisibility);
        Assert.False(viewModel.IsWifiConnectSupported);
    }

    [Fact]
    public void Share_is_supported_only_when_platform_service_and_image_file_exist()
    {
        var imagePath = Path.Combine(Path.GetTempPath(), "ScannerZero.Tests", Guid.NewGuid().ToString("N"), "scan.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);
        File.WriteAllBytes(imagePath, [1, 2, 3]);
        PlatformServices.ShareFactory = () => new FakeShareService();

        var payload = new ParsedScanPayload(ContentKind.Text, "ScannerZero", null, null);
        var viewModel = ScanPayloadViewModelFactory.Create("ScannerZero", payload, imagePath, null);

        Assert.True(viewModel.IsShareSupported);
    }

    [Fact]
    public async Task VCard_import_sets_status_for_platform_result()
    {
        PlatformServices.ContactImporterFactory = () => new FakeContactImporter(importResult: true);
        var payload = new ParsedScanPayload(ContentKind.VCard, "BEGIN:VCARD", null, null);
        var viewModel = Assert.IsType<VCardPayloadViewModel>(
            ScanPayloadViewModelFactory.Create("BEGIN:VCARD", payload, null, null));

        await viewModel.ImportContactCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsContactImportSupported);
        Assert.Equal("Opened contact import.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Wifi_connect_sets_status_when_connector_is_missing()
    {
        var payload = new ParsedScanPayload(
            ContentKind.WiFi,
            "ScannerZero Wi-Fi",
            null,
            null,
            new WifiCredentials("ScannerZero", "", WifiSecurity.None, false));
        var viewModel = Assert.IsType<WifiPayloadViewModel>(
            ScanPayloadViewModelFactory.Create("raw", payload, null, null));

        await viewModel.ConnectWifiCommand.ExecuteAsync(null);

        Assert.Equal("Wi-Fi auto-connect isn't supported on this platform.", viewModel.StatusMessage);
    }

    public void Dispose()
    {
        PlatformServices.ContactImporterFactory = null;
        PlatformServices.ShareFactory = null;
        PlatformServices.WifiConnectorFactory = null;
    }

    private sealed class FakeShareService : IShareService
    {
        public Task ShareImageAsync(string imagePath) => Task.CompletedTask;
    }

    private sealed class FakeContactImporter(bool importResult) : IContactImporter
    {
        public Task<bool> ImportVCardAsync(string vcard) => Task.FromResult(importResult);
    }
}
