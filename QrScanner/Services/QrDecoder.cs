using System;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace QrScanner.Services;

/// <summary>Shared QR decoding logic used by every platform's camera capture implementation.</summary>
public static class QrDecoder
{
    private static readonly BarcodeReader Reader = new()
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
            return Reader.Decode(bitmap)?.Text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Decodes a QR code from raw image bytes (JPEG, PNG, HEIC, WebP, etc.) quickly using progressive downscaling.
    /// Returns the decoded raw QR text and an optimized JPEG representation for storage/display.
    /// </summary>
    public static (string? RawText, byte[]? JpegBytes) DecodeImageBytes(byte[] imageBytes)
    {
        try
        {
            using var original = SKBitmap.Decode(imageBytes);
            if (original is null)
                return (null, null);

            var maxDim = Math.Max(original.Width, original.Height);
            string? text = null;

            // Step 1: If the image is large (> 1280px), scan a downscaled version first (fast path ~10-25ms)
            if (maxDim > 1280)
            {
                var scale = 1280.0f / maxDim;
                var targetW = Math.Max(1, (int)Math.Round(original.Width * scale));
                var targetH = Math.Max(1, (int)Math.Round(original.Height * scale));

                using var downscaled = original.Resize(new SKImageInfo(targetW, targetH), SKSamplingOptions.Default);
                if (downscaled is not null)
                {
                    text = TryDecode(downscaled);
                }
            }

            // Step 2: Fallback to full resolution if the downscaled attempt didn't find a code
            if (string.IsNullOrEmpty(text))
            {
                text = TryDecode(original);
            }

            if (string.IsNullOrEmpty(text))
            {
                return (null, null);
            }

            // Step 3: Produce an optimized JPEG for disk storage and UI preview (capped at 1600px)
            SKBitmap displayBitmap = original;
            bool shouldDisposeDisplay = false;

            if (maxDim > 1600)
            {
                var scale = 1600.0f / maxDim;
                var targetW = Math.Max(1, (int)Math.Round(original.Width * scale));
                var targetH = Math.Max(1, (int)Math.Round(original.Height * scale));
                displayBitmap = original.Resize(new SKImageInfo(targetW, targetH), SKSamplingOptions.Default) ?? original;
                shouldDisposeDisplay = !ReferenceEquals(displayBitmap, original);
            }

            try
            {
                using var image = SKImage.FromBitmap(displayBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                var jpegBytes = data.ToArray();
                return (text, jpegBytes);
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
}
