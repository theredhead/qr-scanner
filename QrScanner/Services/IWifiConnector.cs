using System.Threading.Tasks;
using QrScanner.Models;

namespace QrScanner.Services;

/// <summary>Platform-specific programmatic Wi-Fi connection (join a network from scanned credentials).</summary>
public interface IWifiConnector
{
    /// <summary>Returns true if a connection attempt was successfully initiated/completed.</summary>
    Task<bool> ConnectAsync(WifiCredentials credentials);
}
