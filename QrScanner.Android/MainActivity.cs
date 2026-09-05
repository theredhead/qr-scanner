using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Window;
using Avalonia;
using Avalonia.Android;
using QrScanner.Android.Services;
using QrScanner.Services;
using QrScanner.Views;

namespace QrScanner.Android;

[Activity(
    Label = "QrScanner.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private BackCallback? _backCallback;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        PlatformServices.CameraFactory = () => new AndroidCameraScanService(this);
        PlatformServices.WifiConnectorFactory = () => new AndroidWifiConnector(this);
        PlatformServices.ShareFactory = () => new AndroidShareService(this);
        base.OnCreate(savedInstanceState);

        if ((int)Build.VERSION.SdkInt >= 33)
        {
            _backCallback = new BackCallback(HandleBackInvoked);
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                IOnBackInvokedDispatcher.PriorityDefault,
                _backCallback);
        }
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

    public override void OnBackPressed()
    {
        HandleBackInvoked();
    }

    private void HandleBackInvoked()
    {
        if (MainView.Current?.TryNavigateBack() != true)
            Finish();
    }

    private sealed class BackCallback(Action callback) : Java.Lang.Object, IOnBackInvokedCallback
    {
        public void OnBackInvoked() => callback();
    }
}
