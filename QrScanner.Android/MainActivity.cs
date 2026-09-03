using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using QrScanner.Android.Services;
using QrScanner.Services;

namespace QrScanner.Android;

[Activity(
    Label = "QrScanner.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        PlatformServices.CameraFactory = () => new AndroidCameraScanService(this);
        PlatformServices.WifiConnectorFactory = () => new AndroidWifiConnector(this);
        base.OnCreate(savedInstanceState);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == CameraPermissionBridge.RequestCode)
        {
            var granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
            CameraPermissionBridge.PendingResult?.Invoke(granted);
            CameraPermissionBridge.PendingResult = null;
        }
    }
}
