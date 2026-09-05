using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Util;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Google.Common.Util.Concurrent;
using Java.Lang;
using QrScanner.Services;
using SkiaSharp;
using Exception = System.Exception;

namespace QrScanner.Android.Services;

/// <summary>
/// Android camera capture using CameraX: a native <see cref="PreviewView"/> shows the live feed
/// while a parallel <see cref="ImageAnalysis"/> use case feeds grayscale frames to the shared decoder.
/// </summary>
public sealed class AndroidCameraScanService : Java.Lang.Object, ICameraScanService, ImageAnalysis.IAnalyzer
{
    private readonly Activity _activity;
    private PreviewView? _previewView;
    private ProcessCameraProvider? _cameraProvider;
    private DateTime _lastDecodeAttemptUtc = DateTime.MinValue;
    private bool _hasLoggedFrame;
    private volatile bool _shouldBeRunning;

    public event EventHandler<QrDetectedEventArgs>? QrDetected;

    public AndroidCameraScanService(Activity activity)
    {
        _activity = activity;
    }

    private PreviewView GetOrCreatePreviewView()
    {
        if (_previewView is not null)
        {
            try
            {
                if (_previewView.Handle != IntPtr.Zero)
                {
                    _ = _previewView.SurfaceProvider;
                    return _previewView;
                }
            }
            catch (ObjectDisposedException)
            {
                _previewView = null;
            }
        }

        _previewView = new PreviewView(_activity);
        _previewView.SetImplementationMode(global::AndroidX.Camera.View.PreviewView.ImplementationMode.Compatible!);
        return _previewView;
    }

    public Control CreatePreviewControl() => new AndroidPreviewHost(GetOrCreatePreviewView);

    public Task<bool> RequestPermissionAsync()
    {
        if (ContextCompat.CheckSelfPermission(_activity, Manifest.Permission.Camera) == Permission.Granted)
        {
            return Task.FromResult(true);
        }

        var tcs = new TaskCompletionSource<bool>();
        CameraPermissionBridge.PendingResult = granted => tcs.TrySetResult(granted);
        _activity.RequestPermissions([Manifest.Permission.Camera!], CameraPermissionBridge.RequestCode);
        return tcs.Task;
    }

    public async Task StartAsync()
    {
        _shouldBeRunning = true;
        try
        {
            var previewView = GetOrCreatePreviewView();
            _activity.RunOnUiThread(() =>
            {
                if (previewView.Handle != IntPtr.Zero)
                {
                    previewView.Visibility = global::Android.Views.ViewStates.Visible;
                }
            });

            var future = ProcessCameraProvider.GetInstance(_activity);
            _cameraProvider = await AwaitFutureAsync(future, _activity).ConfigureAwait(true);

            if (!_shouldBeRunning)
            {
                _cameraProvider?.UnbindAll();
                _activity.RunOnUiThread(() =>
                {
                    if (previewView.Handle != IntPtr.Zero)
                    {
                        previewView.Visibility = global::Android.Views.ViewStates.Gone;
                    }
                });
                return;
            }

            var preview = new Preview.Builder().Build();
            preview.SetSurfaceProvider(ContextCompat.GetMainExecutor(_activity)!, previewView.SurfaceProvider);

            var analysis = new ImageAnalysis.Builder()
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .Build();
            analysis.SetAnalyzer(ContextCompat.GetMainExecutor(_activity)!, this);

            _cameraProvider!.UnbindAll();
            var camera = _cameraProvider.BindToLifecycle((ILifecycleOwner)_activity, CameraSelector.DefaultBackCamera, preview, analysis);
            Log.Info("QrScanner", $"Camera bound successfully: {camera}");
        }
        catch (Exception ex)
        {
            Log.Error("QrScanner", $"StartAsync failed: {ex}");
            throw;
        }
    }

    public Task StopAsync()
    {
        _shouldBeRunning = false;
        _activity.RunOnUiThread(() =>
        {
            if (_previewView is not null && _previewView.Handle != IntPtr.Zero)
            {
                _previewView.Visibility = global::Android.Views.ViewStates.Gone;
            }
        });
        _cameraProvider?.UnbindAll();
        return Task.CompletedTask;
    }

    public global::Android.Util.Size? DefaultTargetResolution => null;

    public int TargetCoordinateSystem => 0;

    public void Analyze(IImageProxy image)
    {
        try
        {
            if (!_hasLoggedFrame)
            {
                _hasLoggedFrame = true;
                Log.Info("QrScanner", $"First analysis frame received: {image.Width}x{image.Height}, format={image.Format}");
            }

            var now = DateTime.UtcNow;
            if (now - _lastDecodeAttemptUtc < TimeSpan.FromMilliseconds(250))
            {
                return;
            }
            _lastDecodeAttemptUtc = now;

            using var bitmap = YPlaneToGrayscaleBitmap(image);
            if (bitmap is null)
            {
                return;
            }

            var text = QrDecoder.TryDecode(bitmap);
            if (text is not null)
            {
                using var jpeg = bitmap.Encode(SKEncodedImageFormat.Jpeg, 85);
                QrDetected?.Invoke(this, new QrDetectedEventArgs { RawText = text, JpegImage = jpeg.ToArray() });
            }
        }
        catch (Exception ex)
        {
            Log.Error("QrScanner", $"Frame analysis failed: {ex}");
        }
        finally
        {
            image.Close();
        }
    }

    /// <summary>
    /// Builds a grayscale bitmap from the analysis frame's Y (luma) plane. Uses a standard
    /// 4-byte-per-pixel BGRA format (R=G=B=luma) rather than Gray8, since ZXing's SkiaSharp
    /// binding assumes a fixed bytes-per-pixel layout and silently fails to decode otherwise.
    /// </summary>
    private static SKBitmap? YPlaneToGrayscaleBitmap(IImageProxy image)
    {
        var planes = image.GetPlanes();
        if (planes is null || planes.Length == 0)
        {
            return null;
        }

        var yPlane = planes[0];
        var buffer = yPlane.Buffer;
        if (buffer is null)
        {
            return null;
        }

        var rowStride = yPlane.RowStride;
        var width = image.Width;
        var height = image.Height;

        var yBytes = new byte[buffer.Remaining()];
        buffer.Get(yBytes);

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        var bitmap = new SKBitmap(info);
        var dest = bitmap.GetPixels();
        var destStride = bitmap.RowBytes;

        var row = new byte[width * 4];
        for (var y = 0; y < height; y++)
        {
            var srcOffset = y * rowStride;
            for (var x = 0; x < width; x++)
            {
                var luma = yBytes[srcOffset + x];
                var o = x * 4;
                row[o] = luma;
                row[o + 1] = luma;
                row[o + 2] = luma;
                row[o + 3] = 255;
            }

            Marshal.Copy(row, 0, IntPtr.Add(dest, y * destStride), width * 4);
        }

        return bitmap;
    }

    private static Task<ProcessCameraProvider> AwaitFutureAsync(IListenableFuture future, Activity activity)
    {
        var tcs = new TaskCompletionSource<ProcessCameraProvider>();
        future.AddListener(new Runnable(() =>
        {
            try
            {
                var result = future.Get();
                tcs.TrySetResult((ProcessCameraProvider)result!);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }), ContextCompat.GetMainExecutor(activity));
        return tcs.Task;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cameraProvider?.UnbindAll();
            _cameraProvider?.Dispose();
            _cameraProvider = null;
            _previewView?.Dispose();
            _previewView = null;
        }
        base.Dispose(disposing);
    }

    /// <summary>Hosts the CameraX <see cref="PreviewView"/> inside the Avalonia visual tree.</summary>
    private sealed class AndroidPreviewHost : NativeControlHost
    {
        private readonly Func<global::Android.Views.View> _viewProvider;

        public AndroidPreviewHost(Func<global::Android.Views.View> viewProvider) => _viewProvider = viewProvider;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            Log.Info("QrScanner", "AndroidPreviewHost.CreateNativeControlCore called - attaching PreviewView.");
            return new global::Avalonia.Android.AndroidViewControlHandle(_viewProvider());
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            // The PreviewView's lifetime is owned by AndroidCameraScanService, not by this host.
        }
    }
}
