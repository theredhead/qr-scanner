using System.Threading.Tasks;
using NetworkExtension;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.iOS.Services;

/// <summary>
/// Joins a Wi-Fi network via NEHotspotConfiguration. Requires the "Hotspot Configuration"
/// capability to be enabled for the app's App ID in the Apple Developer portal, and the matching
/// entitlement in Entitlements.plist - without both, ApplyConfiguration fails with a permissions error.
/// </summary>
public sealed class IosWifiConnector : IWifiConnector
{
    public Task<bool> ConnectAsync(WifiCredentials credentials)
    {
        if (credentials.Security == WifiSecurity.None)
        {
            return ApplyAsync(new NEHotspotConfiguration(credentials.Ssid));
        }

        var isWep = credentials.Security == WifiSecurity.Wep;
        return ApplyAsync(new NEHotspotConfiguration(credentials.Ssid, credentials.Password, isWep));
    }

    private static Task<bool> ApplyAsync(NEHotspotConfiguration configuration)
    {
        configuration.JoinOnce = false;

        var tcs = new TaskCompletionSource<bool>();
        NEHotspotConfigurationManager.SharedManager.ApplyConfiguration(configuration, error =>
        {
            tcs.TrySetResult(error is null);
        });
        return tcs.Task;
    }
}
