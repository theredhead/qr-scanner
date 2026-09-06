using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.Android.Services;

/// <summary>
/// Suggests a Wi-Fi network via the system "Add networks" panel (Android 11+ / API 30+), which
/// shows the user a native confirmation dialog. Older Android versions aren't supported here since
/// the legacy WifiManager APIs for adding networks are deprecated and increasingly restricted.
/// </summary>
public sealed class AndroidWifiConnector(Activity activity) : IWifiConnector
{
    public Task<bool> ConnectAsync(WifiCredentials credentials)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            return Task.FromResult(false);
        }

        if (credentials.Security == WifiSecurity.Wep)
        {
            // WifiNetworkSuggestion has no WEP support; WEP is obsolete/insecure and not worth adding.
            return Task.FromResult(false);
        }

        var builder = new WifiNetworkSuggestion.Builder()
            .SetSsid(credentials.Ssid)
            .SetIsHiddenSsid(credentials.Hidden);

        if (credentials.Security == WifiSecurity.Wpa && !string.IsNullOrEmpty(credentials.Password))
        {
            builder.SetWpa2Passphrase(credentials.Password);
        }

        var suggestion = builder.Build();
        if (suggestion is null)
        {
            return Task.FromResult(false);
        }

        var suggestions = new List<IParcelable> { suggestion };
        var bundle = new Bundle();
        bundle.PutParcelableArrayList(Settings.ExtraWifiNetworkList, suggestions);

        var intent = new Intent(Settings.ActionWifiAddNetworks);
        intent.PutExtras(bundle);
        activity.StartActivity(intent);

        return Task.FromResult(true);
    }
}
