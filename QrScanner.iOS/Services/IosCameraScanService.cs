using System;
using System.Threading.Tasks;
using AVFoundation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CoreGraphics;
using CoreVideo;
using Foundation;
using QrScanner.Services;
using SkiaSharp;
using UIKit;

namespace QrScanner.iOS.Services;

/// <summary>
/// iOS camera capture using AVFoundation: an <see cref="AVCaptureVideoPreviewLayer"/> shows the
/// live feed while a parallel <see cref="AVCaptureVideoDataOutput"/> feeds BGRA frames to the
/// shared decoder.
/// </summary>
public sealed class IosCameraScanService : NSObject, ICameraScanService, IAVCaptureVideoDataOutputSampleBufferDelegate
{
    private readonly AVCaptureSession _session = new();
    private readonly UIView _previewContainer = new();
    private readonly AVCaptureVideoPreviewLayer _previewLayer;
    private DateTime _lastDecodeAttemptUtc = DateTime.MinValue;
    private bool _configured;

    public event EventHandler<QrDetectedEventArgs>? QrDetected;

    public IosCameraScanService()
    {
        _previewLayer = new AVCaptureVideoPreviewLayer(_session)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };
        _previewContainer.Layer.AddSublayer(_previewLayer);
    }

    public Control CreatePreviewControl() => new IosPreviewHost(_previewContainer, _previewLayer);

    public Task<bool> RequestPermissionAsync()
    {
        var status = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);
        if (status == AVAuthorizationStatus.Authorized)
        {
            return Task.FromResult(true);
        }

        var tcs = new TaskCompletionSource<bool>();
        AVCaptureDevice.RequestAccessForMediaType(AVAuthorizationMediaType.Video, granted => tcs.TrySetResult(granted));
        return tcs.Task;
    }

    public Task StartAsync()
    {
        if (!_configured)
        {
            Configure();
        }

        if (!_session.Running)
        {
            _session.StartRunning();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_session.Running)
        {
            _session.StopRunning();
        }

        return Task.CompletedTask;
    }

    private void Configure()
    {
        _configured = true;

        var device = AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Back);
        if (device is null)
        {
            return;
        }

        _session.BeginConfiguration();
        _session.SessionPreset = AVCaptureSession.Preset640x480;

        if (AVCaptureDeviceInput.FromDevice(device, out var error) is { } input && _session.CanAddInput(input))
        {
            _session.AddInput(input);
        }

        var output = new AVCaptureVideoDataOutput
        {
            WeakVideoSettings = new NSDictionary(CVPixelBuffer.PixelFormatTypeKey, new NSNumber((int)CVPixelFormatType.CV32BGRA))
        };
        output.SetSampleBufferDelegateQueue(this, new global::Foundation.DispatchQueue("QrScanner.CameraQueue"));

        if (_session.CanAddOutput(output))
        {
            _session.AddOutput(output);
        }

        _session.CommitConfiguration();
    }

    [Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
    public void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CoreMedia.CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            var now = DateTime.UtcNow;
            if (now - _lastDecodeAttemptUtc < TimeSpan.FromMilliseconds(250))
            {
                return;
            }
            _lastDecodeAttemptUtc = now;

            using var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
            if (pixelBuffer is null)
            {
                return;
            }

            using var bitmap = BgraPixelBufferToBitmap(pixelBuffer);
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
        finally
        {
            sampleBuffer.Dispose();
        }
    }

    private static SKBitmap? BgraPixelBufferToBitmap(CVPixelBuffer pixelBuffer)
    {
        pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
        try
        {
            var width = (int)pixelBuffer.Width;
            var height = (int)pixelBuffer.Height;
            var rowBytes = (int)pixelBuffer.BytesPerRow;
            var baseAddress = pixelBuffer.BaseAddress;
            if (baseAddress == IntPtr.Zero)
            {
                return null;
            }

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888);
            var bitmap = new SKBitmap();
            using var pixmap = new SKPixmap(info, baseAddress, rowBytes);
            bitmap.InstallPixels(pixmap);
            return bitmap.Copy();
        }
        finally
        {
            pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    /// <summary>Hosts the AVFoundation preview layer inside the Avalonia visual tree.</summary>
    private sealed class IosPreviewHost : NativeControlHost
    {
        private readonly UIView _view;
        private readonly AVCaptureVideoPreviewLayer _previewLayer;

        public IosPreviewHost(UIView view, AVCaptureVideoPreviewLayer previewLayer)
        {
            _view = view;
            _previewLayer = previewLayer;
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            _previewLayer.Frame = _view.Bounds;
            return new global::Avalonia.iOS.UIViewControlHandle(_view);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            // The preview view's lifetime is owned by IosCameraScanService, not by this host.
        }
    }
}
