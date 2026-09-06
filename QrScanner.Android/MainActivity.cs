using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
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
    private const string Tag = "QrScannerIntent";
    private BackCallback? _backCallback;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        PlatformServices.CameraFactory = () => new AndroidCameraScanService(this);
        PlatformServices.WifiConnectorFactory = () => new AndroidWifiConnector(this);
        PlatformServices.ShareFactory = () => new AndroidShareService(this);

        // Process incoming share intent BEFORE base.OnCreate so image is queued
        // into ExternalImageHandler before MainViewModel initializes its camera state.
        if (Intent is not null)
        {
            HandleIntent(Intent);
        }

        base.OnCreate(savedInstanceState);

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            _backCallback = new BackCallback(HandleBackInvoked);
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                IOnBackInvokedDispatcher.PriorityDefault,
                _backCallback);
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
        Log.Info(Tag, $"HandleIntent received action: {action}, type: {intent.Type}");

        if (action == Intent.ActionSend)
        {
            global::Android.Net.Uri? uri = null;

            if (intent.ClipData is { ItemCount: > 0 })
            {
                uri = intent.ClipData.GetItemAt(0)?.Uri;
                Log.Info(Tag, $"Got URI from ClipData: {uri}");
            }

            if (uri is null)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                {
                    uri = intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri))) as global::Android.Net.Uri;
                }
                else
                {
#pragma warning disable CA1422
                    uri = intent.GetParcelableExtra(Intent.ExtraStream) as global::Android.Net.Uri;
#pragma warning restore CA1422
                }
                Log.Info(Tag, $"Got URI from ExtraStream: {uri}");
            }

            if (uri is null)
            {
                uri = intent.Data;
                Log.Info(Tag, $"Got URI from Data: {uri}");
            }

            if (uri is not null)
            {
                ReadAndProcessImageUri(uri);
            }
            else
            {
                Log.Warn(Tag, "ActionSend was received but no image URI could be extracted.");
            }
        }
        else if (action == Intent.ActionSendMultiple)
        {
            if (intent.ClipData is { ItemCount: > 0 })
            {
                var uri = intent.ClipData.GetItemAt(0)?.Uri;
                if (uri is not null)
                {
                    ReadAndProcessImageUri(uri);
                    return;
                }
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var uris = intent.GetParcelableArrayListExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri)));
                if (uris is { Count: > 0 } && uris[0] is global::Android.Net.Uri uriFirst)
                {
                    ReadAndProcessImageUri(uriFirst);
                }
            }
            else
            {
#pragma warning disable CA1422
                var uris = intent.GetParcelableArrayListExtra(Intent.ExtraStream);
                if (uris is { Count: > 0 } && uris[0] is global::Android.Net.Uri uriFirst)
                {
                    ReadAndProcessImageUri(uriFirst);
                }
#pragma warning restore CA1422
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
            Log.Info(Tag, $"Reading stream for URI: {uri}");
            using var stream = ContentResolver?.OpenInputStream(uri);
            if (stream is not null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var bytes = ms.ToArray();
                Log.Info(Tag, $"Read {bytes.Length} bytes from shared image URI.");
                if (bytes.Length > 0)
                {
                    ExternalImageHandler.HandleImage(bytes);
                }
            }
            else
            {
                Log.Warn(Tag, $"ContentResolver returned null stream for URI: {uri}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(Tag, $"Failed to read shared image URI: {ex}");
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
