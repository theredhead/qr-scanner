using System.Threading.Tasks;

namespace ScannerZero.Services;

/// <summary>Platform-specific contact import for scanned vCard payloads.</summary>
public interface IContactImporter
{
    /// <summary>Returns true if a native contact import or creation flow was started.</summary>
    Task<bool> ImportVCardAsync(string vCard);
}
