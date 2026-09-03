using System;

namespace QrScanner.Android.Services;

/// <summary>
/// Bridges MainActivity.OnRequestPermissionsResult (which we don't control the call site of)
/// back to whichever AndroidCameraScanService is currently awaiting a permission result.
/// </summary>
public static class CameraPermissionBridge
{
    public const int RequestCode = 4242;

    public static Action<bool>? PendingResult { get; set; }
}
