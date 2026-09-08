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
using Java.Util.Concurrent;
using ScannerZero.Services;
using SkiaSharp;
using Exception = System.Exception;

namespace ScannerZero.Android.Services;

/// <summary>
/// Android camera capture using CameraX: a native <see cref="PreviewView"/> shows the live feed
/// while a parallel <see cref="ImageAnalysis"/> use case feeds grayscale frames to the shared decoder.
/// </summary>
public sealed class AndroidCameraScanService : Java.Lang.Object, ICameraScanService, ImageAnalysis.IAnalyzer
{
    private readonly Activity _activity;
    private PreviewView? _previewView;
    private global::Android.Widget.FrameLayout? _container;
    private PinchZoomTouchListener? _pinchZoomTouchListener;
    private ICamera? _camera;
    private ProcessCameraProvider? _cameraProvider;
    private IExecutorService? _analysisExecutor;
    private DateTime _lastDecodeAttemptUtc = DateTime.MinValue;
    private float _linearZoom;
    private bool _hasLoggedFrame;
    private volatile bool _shouldBeRunning;

    public event EventHandler<CodeDetectedEventArgs>? CodeDetected;

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

    private global::Android.Views.View GetOrCreateContainer()
    {
        if (_container is not null && _container.Handle != IntPtr.Zero)
        {
            return _container;
        }

        var preview = GetOrCreatePreviewView();
        if (preview.Parent is global::Android.Views.ViewGroup oldParent)
        {
            oldParent.RemoveView(preview);
        }

        _container = new global::Android.Widget.FrameLayout(_activity);
        _container.AddView(preview, new global::Android.Widget.FrameLayout.LayoutParams(
            global::Android.Views.ViewGroup.LayoutParams.MatchParent,
            global::Android.Views.ViewGroup.LayoutParams.MatchParent));

        var overlay = new ViewfinderOverlayView(_activity);
        _pinchZoomTouchListener ??= new PinchZoomTouchListener(_activity, AdjustZoom);
        overlay.Clickable = true;
        overlay.SetOnTouchListener(_pinchZoomTouchListener);
        _container.AddView(overlay, new global::Android.Widget.FrameLayout.LayoutParams(
            global::Android.Views.ViewGroup.LayoutParams.MatchParent,
            global::Android.Views.ViewGroup.LayoutParams.MatchParent));

        return _container;
    }

    public Control CreatePreviewControl() => new AndroidPreviewHost(GetOrCreateContainer);

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

            var preview = new Preview.Builder()?.Build();
            if (preview is not null && previewView.SurfaceProvider is not null)
            {
                var executor = ContextCompat.GetMainExecutor(_activity);
                if (executor is not null)
                {
                    preview.SetSurfaceProvider(executor, previewView.SurfaceProvider);
                }
            }

            var resolutionSelectorBuilder = new global::AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector.Builder();
            resolutionSelectorBuilder.SetResolutionStrategy(new global::AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy(
                new global::Android.Util.Size(1280, 720),
                global::AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy.FallbackRuleClosestHigherThenLower));
            var resolutionSelector = resolutionSelectorBuilder.Build();

            var analysis = new ImageAnalysis.Builder()
                ?.SetResolutionSelector(resolutionSelector)
                ?.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                ?.Build();

            if (analysis is not null)
            {
                _analysisExecutor ??= Executors.NewSingleThreadExecutor();
                analysis.SetAnalyzer(_analysisExecutor, this);
            }

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

            if (_cameraProvider is not null && _activity is ILifecycleOwner lifecycleOwner && preview is not null && analysis is not null)
            {
                _cameraProvider.UnbindAll();
                var camera = _cameraProvider.BindToLifecycle(lifecycleOwner, CameraSelector.DefaultBackCamera!, preview, analysis);
                _camera = camera;
                ApplyZoom(_linearZoom);
                if (_shouldBeRunning)
                {
                    Log.Info("ScannerZero", $"Camera bound successfully: {camera}");
                }
                else
                {
                    _cameraProvider.UnbindAll();
                    _activity.RunOnUiThread(() =>
                    {
                        if (previewView.Handle != IntPtr.Zero)
                        {
                            previewView.Visibility = global::Android.Views.ViewStates.Gone;
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("ScannerZero", $"StartAsync failed: {ex}");
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
        _camera = null;
        return Task.CompletedTask;
    }

    public global::Android.Util.Size? DefaultTargetResolution => null;

    public int TargetCoordinateSystem => 0;

    public async void Analyze(IImageProxy? image)
    {
        if (image is null)
            return;

        try
        {
            if (!_shouldBeRunning)
            {
                return;
            }

            if (!_hasLoggedFrame)
            {
                _hasLoggedFrame = true;
                Log.Info("ScannerZero", $"First analysis frame received: {image.Width}x{image.Height}, format={image.Format}");
            }

            var now = DateTime.UtcNow;
            if (now - _lastDecodeAttemptUtc < TimeSpan.FromMilliseconds(250))
            {
                return;
            }
            _lastDecodeAttemptUtc = now;

            using var cameraBitmap = YPlaneToGrayscaleBitmap(image);
            if (cameraBitmap is null)
            {
                return;
            }

            var result = await BarcodeDecoder.Scan(cameraBitmap).ConfigureAwait(false);
            if (result.IsSuccess && result.RawText is not null)
            {
                using var rotatedBitmap = RotateToDisplayOrientation(cameraBitmap, image.ImageInfo?.RotationDegrees ?? 0);
                var bitmapToSave = rotatedBitmap ?? cameraBitmap;
                using var jpeg = bitmapToSave.Encode(SKEncodedImageFormat.Jpeg, 85);
                CodeDetected?.Invoke(this, new CodeDetectedEventArgs
                {
                    RawText = result.RawText,
                    StrategyId = result.StrategyId,
                    StrategyName = result.StrategyName,
                    CodeType = result.CodeType,
                    JpegImage = jpeg.ToArray()
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error("ScannerZero", $"Frame analysis failed: {ex}");
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

    private static SKBitmap? RotateToDisplayOrientation(SKBitmap source, int degrees)
    {
        degrees = ((degrees % 360) + 360) % 360;
        if (degrees == 0)
        {
            return null;
        }

        var swapDimensions = degrees is 90 or 270;
        var targetWidth = swapDimensions ? source.Height : source.Width;
        var targetHeight = swapDimensions ? source.Width : source.Height;
        var rotated = new SKBitmap(new SKImageInfo(targetWidth, targetHeight, source.ColorType, source.AlphaType));

        using var canvas = new SKCanvas(rotated);
        canvas.Translate(targetWidth / 2f, targetHeight / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-source.Width / 2f, -source.Height / 2f);
        canvas.DrawBitmap(source, 0, 0, SKSamplingOptions.Default);

        return rotated;
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
            _camera = null;
            _analysisExecutor?.Shutdown();
            _analysisExecutor?.Dispose();
            _analysisExecutor = null;
            _previewView?.Dispose();
            _previewView = null;
        }
        base.Dispose(disposing);
    }

    private void AdjustZoom(float scaleFactor)
    {
        if (_camera is null || scaleFactor <= 0)
        {
            return;
        }

        var targetZoom = global::System.Math.Clamp(_linearZoom + ((scaleFactor - 1f) * 0.35f), 0f, 1f);
        if (global::System.Math.Abs(targetZoom - _linearZoom) < 0.001f)
        {
            return;
        }

        _linearZoom = targetZoom;
        ApplyZoom(_linearZoom);
    }

    private void ApplyZoom(float linearZoom)
    {
        try
        {
            _camera?.CameraControl?.SetLinearZoom(linearZoom);
        }
        catch (Exception ex)
        {
            Log.Warn("ScannerZero", $"Zoom failed: {ex}");
        }
    }

    /// <summary>Hosts the CameraX <see cref="PreviewView"/> and native viewfinder overlay inside the Avalonia visual tree.</summary>
    private sealed class AndroidPreviewHost : NativeControlHost
    {
        private readonly Func<global::Android.Views.View> _viewProvider;

        public AndroidPreviewHost(Func<global::Android.Views.View> viewProvider) => _viewProvider = viewProvider;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            Log.Info("ScannerZero", "AndroidPreviewHost.CreateNativeControlCore called - attaching PreviewView.");
            var view = _viewProvider();
            if (view.Parent is global::Android.Views.ViewGroup currentParent)
            {
                currentParent.RemoveView(view);
            }
            return new global::Avalonia.Android.AndroidViewControlHandle(view);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            try
            {
                var view = _viewProvider();
                if (view.Parent is global::Android.Views.ViewGroup currentParent)
                {
                    currentParent.RemoveView(view);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("ScannerZero", $"DestroyNativeControlCore exception ignored: {ex}");
            }
        }
    }

    /// <summary>Draws the native viewfinder target box directly on top of the CameraX PreviewView.</summary>
    private sealed class ViewfinderOverlayView : global::Android.Views.View
    {
        private readonly global::Android.Graphics.Paint _borderPaint;
        private readonly float _boxSizeDp;
        private readonly float _cornerRadiusDp;

        public ViewfinderOverlayView(global::Android.Content.Context context) : base(context)
        {
            SetWillNotDraw(false);
            var density = context.Resources?.DisplayMetrics?.Density ?? 1f;
            _boxSizeDp = 260f * density;
            _cornerRadiusDp = 20f * density;

            _borderPaint = new global::Android.Graphics.Paint
            {
                Color = new global::Android.Graphics.Color(255, 62, 28), // ScannerZero accent (#FF3E1C)
                StrokeWidth = 3f * density,
                AntiAlias = true
            };
            _borderPaint.SetStyle(global::Android.Graphics.Paint.Style.Stroke);
        }

        protected override void OnDraw(global::Android.Graphics.Canvas canvas)
        {
            base.OnDraw(canvas);

            var w = Width;
            var h = Height;
            var left = (w - _boxSizeDp) / 2f;
            var top = (h - _boxSizeDp) / 2f;
            var right = left + _boxSizeDp;
            var bottom = top + _boxSizeDp;

            var rect = new global::Android.Graphics.RectF(left, top, right, bottom);
            canvas.DrawRoundRect(rect, _cornerRadiusDp, _cornerRadiusDp, _borderPaint);
        }
    }

    private sealed class PinchZoomTouchListener : Java.Lang.Object, global::Android.Views.View.IOnTouchListener
    {
        private readonly global::Android.Views.ScaleGestureDetector _scaleDetector;

        public PinchZoomTouchListener(global::Android.Content.Context context, Action<float> onScale)
        {
            _scaleDetector = new global::Android.Views.ScaleGestureDetector(
                context,
                new PinchScaleListener(onScale));
        }

        public bool OnTouch(global::Android.Views.View? view, global::Android.Views.MotionEvent? motionEvent)
        {
            if (motionEvent is null)
            {
                return false;
            }

            _scaleDetector.OnTouchEvent(motionEvent);
            return true;
        }
    }

    private sealed class PinchScaleListener(Action<float> onScale) : global::Android.Views.ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        public override bool OnScale(global::Android.Views.ScaleGestureDetector detector)
        {
            onScale(detector.ScaleFactor);
            return true;
        }
    }
}
