using System;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace QrScanner.Services;

/// <summary>Shared QR decoding logic used by every platform's camera capture implementation.</summary>
public static class QrDecoder
{
    private static BarcodeReader CreateReader() => new()
    {
        AutoRotate = true,
        Options = new DecodingOptions
        {
            TryHarder = true,
            TryInverted = true,
            PossibleFormats = [BarcodeFormat.QR_CODE]
        }
    };

    /// <summary>Returns the decoded text, or null if no QR code was found in the bitmap.</summary>
    public static string? TryDecode(SKBitmap bitmap)
    {
        try
        {
            var reader = CreateReader();
            return reader.Decode(bitmap)?.Text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Decodes a QR code from raw image bytes (JPEG, PNG, HEIC, WebP, etc.) quickly using progressive multi-stage decoding.
    /// Returns the decoded raw QR text and an optimized JPEG representation for storage/display.
    /// </summary>
    public static (string? RawText, byte[]? JpegBytes) DecodeImageBytes(byte[] imageBytes)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            return (null, null);

        try
        {
            using var original = DecodeWithAutoOrientation(imageBytes);
            if (original is null)
                return (null, null);

            var maxDim = Math.Max(original.Width, original.Height);
            string? text = null;

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
                // Pass 1: Standard downscaled decode (~15ms)
                text = TryDecode(candidateBitmap);

                // Pass 2: Anti-moire / slight blur filter (~20ms)
                // When photos are taken of monitors, TVs, or other phone screens, subpixel grids create moire patterns
                // that distort standard binarizers. A gentle 1.0px blur smooths screen frequency noise while preserving QR modules.
                if (string.IsNullOrEmpty(text))
                {
                    using var smoothed = new SKBitmap(candidateBitmap.Width, candidateBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                    using (var canvas = new SKCanvas(smoothed))
                    using (var paint = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(1.0f, 1.0f) })
                    {
                        canvas.DrawBitmap(candidateBitmap, 0, 0, SKSamplingOptions.Default, paint);
                    }
                    text = TryDecode(smoothed);
                }

                // Pass 3: Fallback to full native resolution
                if (string.IsNullOrEmpty(text) && !ReferenceEquals(candidateBitmap, original))
                {
                    text = TryDecode(original);
                }
            }
            finally
            {
                if (shouldDisposeCandidate)
                {
                    candidateBitmap.Dispose();
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                return (null, null);
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
                        return (text, jpegBytes);
                    }
                }
                return (text, imageBytes);
            }
            finally
            {
                if (shouldDisposeDisplay)
                {
                    displayBitmap.Dispose();
                }
            }
        }
        catch
        {
            return (null, null);
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
