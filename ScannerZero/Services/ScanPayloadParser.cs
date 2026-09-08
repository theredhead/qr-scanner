using ScannerZero.Models;

namespace ScannerZero.Services;

/// <summary>Interprets raw scanned barcode data into a displayable, actionable form.</summary>
public static class ScanPayloadParser
{
    public static ParsedScanPayload Parse(string raw) => ScanPayloadStrategies.Analyze(raw);
}
