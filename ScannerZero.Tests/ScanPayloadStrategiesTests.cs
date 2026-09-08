using ScannerZero.Models;
using ScannerZero.Services;
using Xunit;

namespace ScannerZero.Tests;

public sealed class ScanPayloadStrategiesTests
{
    [Theory]
    [InlineData("https://github.com/theredhead/qr-scanner", ContentKind.Url, "Open link")]
    [InlineData("mailto:pi@scannerzero.example?subject=ScannerZero", ContentKind.Email, "Send email")]
    [InlineData("tel:+31415926535", ContentKind.Phone, "Call number")]
    [InlineData("ScannerZero payload: pi = 3.141592653589793", ContentKind.Text, null)]
    public void Analyze_detects_simple_payload_types(string raw, ContentKind expectedKind, string? expectedActionLabel)
    {
        var result = ScanPayloadStrategies.Analyze(raw);

        Assert.Equal(expectedKind, result.Kind);
        Assert.Equal(raw, result.DisplayText);
        Assert.Equal(expectedActionLabel, result.ActionLabel);
    }

    [Fact]
    public void Analyze_detects_vcard_payload()
    {
        const string raw = """
            BEGIN:VCARD
            VERSION:3.0
            FN:Scanner Zero
            END:VCARD
            """;

        var result = ScanPayloadStrategies.Analyze(raw);

        Assert.Equal(ContentKind.VCard, result.Kind);
        Assert.Null(result.ActionUri);
    }

    [Fact]
    public void Analyze_parses_wifi_credentials()
    {
        const string raw = @"WIFI:T:WPA;S:ScannerZero\;Pi;P:3.1415926535;H:true;;";

        var result = ScanPayloadStrategies.Analyze(raw);

        Assert.Equal(ContentKind.WiFi, result.Kind);
        Assert.NotNull(result.Wifi);
        Assert.Equal("ScannerZero;Pi", result.Wifi.Ssid);
        Assert.Equal("3.1415926535", result.Wifi.Password);
        Assert.Equal(WifiSecurity.Wpa, result.Wifi.Security);
        Assert.True(result.Wifi.Hidden);
    }

    [Fact]
    public void Analyze_handles_open_wifi_networks()
    {
        const string raw = "WIFI:T:nopass;S:ScannerZero-Guest;P:;H:false;;";

        var result = ScanPayloadStrategies.Analyze(raw);

        Assert.Equal(ContentKind.WiFi, result.Kind);
        Assert.NotNull(result.Wifi);
        Assert.Equal(WifiSecurity.None, result.Wifi.Security);
        Assert.False(result.Wifi.Hidden);
    }

    [Fact]
    public void Parse_delegates_to_payload_strategies()
    {
        var result = ScanPayloadParser.Parse("HTTPS://github.com/theredhead/qr-scanner");

        Assert.Equal(ContentKind.Url, result.Kind);
        Assert.Equal("Open link", result.ActionLabel);
    }

    [Fact]
    public void Analyze_treats_incomplete_wifi_payload_as_wifi_without_credentials()
    {
        var result = ScanPayloadStrategies.Analyze("WIFI:T:WPA;P:pi;;");

        Assert.Equal(ContentKind.WiFi, result.Kind);
        Assert.Null(result.Wifi);
    }

    [Theory]
    [InlineData("WIFI:T:WEP;S:ScannerZero;P:pi;;", WifiSecurity.Wep)]
    [InlineData("WIFI:S:ScannerZero;P:;;", WifiSecurity.None)]
    [InlineData("WIFI:T:;S:ScannerZero;P:;;", WifiSecurity.None)]
    public void Analyze_maps_wifi_security_types(string raw, WifiSecurity expectedSecurity)
    {
        var result = ScanPayloadStrategies.Analyze(raw);

        Assert.NotNull(result.Wifi);
        Assert.Equal(expectedSecurity, result.Wifi.Security);
    }
}
