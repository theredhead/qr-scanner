using System;
using System.IO;
using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;
using QrScanner.iOS.Services;
using QrScanner.Services;

namespace QrScanner.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        PlatformServices.CameraFactory = () => new IosCameraScanService();
        PlatformServices.WifiConnectorFactory = () => new IosWifiConnector();
        PlatformServices.ShareFactory = () => new IosShareService();

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        if (url is not null)
        {
            try
            {
                var shouldStopAccessing = url.StartAccessingSecurityScopedResource();
                try
                {
                    var path = url.Path;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        var bytes = File.ReadAllBytes(path);
                        if (bytes.Length > 0)
                        {
                            ExternalImageHandler.HandleImage(bytes);
                            return true;
                        }
                    }
                }
                finally
                {
                    if (shouldStopAccessing)
                        url.StopAccessingSecurityScopedResource();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open shared image URL: {ex}");
            }
        }

        return base.OpenUrl(app, url, options);
    }
}
