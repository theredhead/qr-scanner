using System;
using System.IO;
using Android.App;
using Android.Content;
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
    Label = "QR Scanner",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
[IntentFilter(
    [Intent.ActionSend],
    Categories = [Intent.CategoryDefault],
    DataMimeType = "image/*")]
[IntentFilter(
    [Intent.ActionSendMultiple],
    Categories = [Intent.CategoryDefault],
    DataMimeType = "image/*")]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataMimeType = "image/*")]
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

        if (Intent is not null)
        {
            HandleIntent(Intent);
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        if (intent is not null)
        {
            HandleIntent(intent);
        }
    }

    private void HandleIntent(Intent intent)
    {
        var action = intent.Action;
        if (action == Intent.ActionSend)
        {
            var uri = (intent.GetParcelableExtra(Intent.ExtraStream) as global::Android.Net.Uri)
                      ?? intent.ClipData?.GetItemAt(0)?.Uri
                      ?? intent.Data;

            if (uri is not null)
            {
                ReadAndProcessImageUri(uri);
            }
        }
        else if (action == Intent.ActionSendMultiple)
        {
            var uris = intent.GetParcelableArrayListExtra(Intent.ExtraStream);
            if (uris is { Count: > 0 } && uris[0] is global::Android.Net.Uri uri)
            {
                ReadAndProcessImageUri(uri);
            }
        }
        else if (action == Intent.ActionView)
        {
            if (intent.Data is { } uri)
            {
                ReadAndProcessImageUri(uri);
            }
        }
    }

    private void ReadAndProcessImageUri(global::Android.Net.Uri uri)
    {
        try
        {
            using var stream = ContentResolver?.OpenInputStream(uri);
            if (stream is not null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var bytes = ms.ToArray();
                if (bytes.Length > 0)
                {
                    ExternalImageHandler.HandleImage(bytes);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read shared image URI: {ex}");
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
