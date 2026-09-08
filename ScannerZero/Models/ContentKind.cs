namespace ScannerZero.Models;

/// <summary>The kind of payload a scanned code was interpreted as.</summary>
public enum ContentKind
{
    Text,
    Url,
    Email,
    Phone,
    WiFi,
    VCard
}
