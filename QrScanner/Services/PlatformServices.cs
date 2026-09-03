using System;

namespace QrScanner.Services;

/// <summary>
/// Simple service locator set up by each platform head (Desktop/Android/iOS Program/Activity/AppDelegate)
/// before the UI starts, so shared view models can obtain a camera implementation without a DI container.
/// </summary>
public static class PlatformServices
{
    public static Func<ICameraScanService>? CameraFactory { get; set; }

    /// <summary>Null on platforms (e.g. Desktop) that don't support programmatic Wi-Fi connection.</summary>
    public static Func<IWifiConnector>? WifiConnectorFactory { get; set; }
}
