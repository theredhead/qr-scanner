namespace ScannerZero.Models;

/// <summary>Result of interpreting the raw text carried by a scanned code.</summary>
public sealed record ParsedScanPayload(
    ContentKind Kind,
    string DisplayText,
    string? ActionUri,
    string? ActionLabel,
    WifiCredentials? Wifi = null);
