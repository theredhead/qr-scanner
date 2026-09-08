using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using ScannerZero.Models;
using ScannerZero.Services;
using SkiaSharp;

namespace ScannerZero.ViewModels;

public sealed partial class ScanResultViewModel : ViewModelBase, IDisposable
{
    private readonly Action? _onDismiss;

    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Bitmap? Image { get; }
    public string? RawText { get; }
    public string DisplayText { get; }
    public ContentKind Kind { get; }
    public string StrategyName { get; }
    public string CodeType { get; }
    public string ScannerBadge => $"{CodeType} via {StrategyName}";
    public string? ImagePath { get; }
    public ScanPayloadViewModel? PayloadActions { get; }

    public string Title => IsSuccess ? "Scan result" : "Scan failed";

    public string KindBadge => Kind switch
    {
        ContentKind.Url => "Website",
        ContentKind.WiFi => "Wi-Fi Network",
        ContentKind.Email => "Email Address",
        ContentKind.Phone => "Phone Number",
        ContentKind.VCard => "Contact Card",
        _ => "Text"
    };

    private ScanResultViewModel(
        bool isSuccess,
        string? errorMessage,
        Bitmap? image,
        string? rawText,
        ParsedScanPayload? parsed,
        string? imagePath,
        string? strategyName,
        string? codeType,
        ScanPayloadViewModel? payloadActions,
        Action? onDismiss)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Image = image;
        RawText = rawText;
        DisplayText = parsed?.DisplayText ?? rawText ?? string.Empty;
        Kind = parsed?.Kind ?? ContentKind.Text;
        ImagePath = imagePath;
        StrategyName = string.IsNullOrWhiteSpace(strategyName) ? "Unknown strategy" : strategyName;
        CodeType = string.IsNullOrWhiteSpace(codeType) ? "Unknown" : codeType;
        PayloadActions = payloadActions;
        _onDismiss = onDismiss;
    }

    public static ScanResultViewModel CreateSuccess(
        string rawText,
        byte[] jpegBytes,
        string imagePath,
        string? strategyName,
        string? codeType,
        Action? onDismiss)
    {
        var parsed = ScanPayloadParser.Parse(rawText);
        Bitmap? bitmap = null;

        try
        {
            if (jpegBytes is { Length: > 0 })
            {
                bitmap = new Bitmap(new MemoryStream(jpegBytes));
            }
            else if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                bitmap = new Bitmap(imagePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load bitmap in CreateSuccess: {ex}");
        }

        var payloadActions = ScanPayloadViewModelFactory.Create(rawText, parsed, imagePath, onDismiss);

        return new ScanResultViewModel(
            isSuccess: true,
            errorMessage: null,
            image: bitmap,
            rawText: rawText,
            parsed: parsed,
            imagePath: imagePath,
            strategyName: strategyName,
            codeType: codeType,
            payloadActions: payloadActions,
            onDismiss: onDismiss);
    }

    public static ScanResultViewModel CreateFailure(
        byte[]? imageBytes,
        string errorMessage,
        Action? onDismiss)
    {
        Bitmap? bitmap = null;
        if (imageBytes is { Length: > 0 })
        {
            try
            {
                bitmap = new Bitmap(new MemoryStream(imageBytes));
            }
            catch
            {
                try
                {
                    using var skBitmap = SKBitmap.Decode(imageBytes);
                    if (skBitmap is not null)
                    {
                        using var image = SKImage.FromBitmap(skBitmap);
                        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                        bitmap = new Bitmap(new MemoryStream(data.ToArray()));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load fallback bitmap in CreateFailure: {ex}");
                }
            }
        }

        return new ScanResultViewModel(
            isSuccess: false,
            errorMessage: errorMessage,
            image: bitmap,
            rawText: null,
            parsed: null,
            imagePath: null,
            strategyName: null,
            codeType: null,
            payloadActions: null,
            onDismiss: onDismiss);
    }

    [RelayCommand]
    private void Dismiss() => _onDismiss?.Invoke();

    public void Dispose()
    {
        Image?.Dispose();
    }
}
