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
}
