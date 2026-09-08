using QrScanner.Models;

namespace QrScanner.Services;

/// <summary>Interprets raw scanned barcode data into a displayable, actionable form.</summary>
public static class QrContentParser
{
    public static ParsedQrContent Parse(string raw) => ScanPayloadStrategies.Analyze(raw);
}
