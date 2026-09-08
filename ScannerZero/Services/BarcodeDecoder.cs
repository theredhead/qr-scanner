using System;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace ScannerZero.Services;

/// <summary>Shared image decoding logic used by camera capture and external image ingestion.</summary>
public static class BarcodeDecoder
{
    /// <summary>Scans a bitmap using the enabled scanner strategies in user-defined order.</summary>
    public static async Task<ScannerResult> Scan(SKBitmap bitmap, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ScannerStrategyRunner.Scan(bitmap, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return ScannerResult.Failure("Scanning failed.");
        }
    }

    /// <summary>
    /// Decodes a barcode from raw image bytes (JPEG, PNG, HEIC, WebP, etc.) quickly using progressive multi-stage decoding.
    /// Returns the decoded raw text and an optimized JPEG representation for storage/display.
    /// </summary>
    public static async Task<(ScannerResult Result, byte[]? JpegBytes)> ScanImageBytes(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            return (ScannerResult.Failure("The selected image is empty."), null);

        try
        {
            using var original = DecodeWithAutoOrientation(imageBytes);
            if (original is null)
                return (ScannerResult.Failure("The selected image could not be read."), null);

            var maxDim = Math.Max(original.Width, original.Height);
            ScannerResult result = ScannerResult.Failure("No enabled barcode strategy could read this image.");

            // Target candidate bitmap to test
            SKBitmap candidateBitmap;
            bool shouldDisposeCandidate = false;

            if (maxDim > 1280)
            {
                var scale = 1280.0f / maxDim;
                var targetW = Math.Max(1, (int)Math.Round(original.Width * scale));
                var targetH = Math.Max(1, (int)Math.Round(original.Height * scale));
                candidateBitmap = original.Resize(new SKImageInfo(targetW, targetH, SKColorType.Rgba8888, SKAlphaType.Premul), SKSamplingOptions.Default) ?? original;
                shouldDisposeCandidate = !ReferenceEquals(candidateBitmap, original);
            }
            else
            {
                candidateBitmap = original;
            }

            try
            {
                // Pass 1: Standard downscaled decode.
                result = await Scan(candidateBitmap, cancellationToken).ConfigureAwait(false);

                // Pass 2: Anti-moire / slight blur filter.
                // When photos are taken of monitors, TVs, or other phone screens, subpixel grids create moire patterns
                // that distort standard binarizers. A gentle 1.0px blur smooths screen frequency noise while preserving code modules.
                if (!result.IsSuccess)
                {
                    using var smoothed = new SKBitmap(candidateBitmap.Width, candidateBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                    using (var canvas = new SKCanvas(smoothed))
                    using (var paint = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(1.0f, 1.0f) })
                    {
                        canvas.DrawBitmap(candidateBitmap, 0, 0, SKSamplingOptions.Default, paint);
                    }
                    result = await Scan(smoothed, cancellationToken).ConfigureAwait(false);
                }

                // Pass 3: Fallback to full native resolution
                if (!result.IsSuccess && !ReferenceEquals(candidateBitmap, original))
                {
                    result = await Scan(original, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (shouldDisposeCandidate)
                {
                    candidateBitmap.Dispose();
                }
            }

            if (!result.IsSuccess)
            {
                return (result, null);
            }

            // Produce an optimized JPEG for disk storage and UI preview (capped at 1600px)
            SKBitmap displayBitmap = original;
            bool shouldDisposeDisplay = false;

            if (maxDim > 1600)
            {
                var scale = 1600.0f / maxDim;
                var targetW = Math.Max(1, (int)Math.Round(original.Width * scale));
                var targetH = Math.Max(1, (int)Math.Round(original.Height * scale));
                displayBitmap = original.Resize(new SKImageInfo(targetW, targetH, SKColorType.Rgba8888, SKAlphaType.Premul), SKSamplingOptions.Default) ?? original;
                shouldDisposeDisplay = !ReferenceEquals(displayBitmap, original);
            }

            try
            {
                using var image = SKImage.FromBitmap(displayBitmap);
                if (image is not null)
                {
                    using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                    if (data is not null)
                    {
                        var jpegBytes = data.ToArray();
                        return (result, jpegBytes);
                    }
                }
                return (result, imageBytes);
            }
            finally
            {
                if (shouldDisposeDisplay)
                {
                    displayBitmap.Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return (ScannerResult.Failure("Scanning failed."), null);
        }
    }

    private static SKBitmap? DecodeWithAutoOrientation(byte[] bytes)
    {
        try
        {
            using var stream = new SKMemoryStream(bytes);
            using var codec = SKCodec.Create(stream);
            if (codec is null)
            {
                return SKBitmap.Decode(bytes);
            }

            var origin = codec.EncodedOrigin;
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);

            var result = codec.GetPixels(info, bitmap.GetPixels());
            if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
            {
                bitmap.Dispose();
                return SKBitmap.Decode(bytes);
            }

            return origin switch
            {
                SKEncodedOrigin.RightTop => Rotate(bitmap, 90),
                SKEncodedOrigin.BottomRight => Rotate(bitmap, 180),
                SKEncodedOrigin.LeftBottom => Rotate(bitmap, 270),
                _ => bitmap
            };
        }
        catch
        {
            return SKBitmap.Decode(bytes);
        }
    }

    private static SKBitmap Rotate(SKBitmap source, float degrees)
    {
        bool swap = degrees is 90 or 270;
        int targetW = swap ? source.Height : source.Width;
        int targetH = swap ? source.Width : source.Height;

        var rotated = new SKBitmap(new SKImageInfo(targetW, targetH, source.ColorType, source.AlphaType));
        using (var canvas = new SKCanvas(rotated))
        {
            canvas.Translate(targetW / 2f, targetH / 2f);
            canvas.RotateDegrees(degrees);
            canvas.Translate(-source.Width / 2f, -source.Height / 2f);
            canvas.DrawBitmap(source, 0, 0, SKSamplingOptions.Default);
        }
        source.Dispose();
        return rotated;
    }
}
