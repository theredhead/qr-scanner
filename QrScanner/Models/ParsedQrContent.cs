namespace QrScanner.Models;

/// <summary>Result of interpreting the raw text carried by a scanned QR code.</summary>
public sealed record ParsedQrContent(
    ContentKind Kind,
    string DisplayText,
    string? ActionUri,
    string? ActionLabel,
    WifiCredentials? Wifi = null);
