using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace ScannerZero.Services;

public abstract class ZxingBarcodeScanStrategy : IScannerStrategy
{
    protected ZxingBarcodeScanStrategy(string id, string displayName, string codeType, BarcodeFormat format)
    {
        Id = id;
        DisplayName = displayName;
        CodeType = codeType;
        Format = format;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string CodeType { get; }

    public BarcodeFormat Format { get; }

    public Task<ScannerResult> Scan(SKBitmap bitmap, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ScanCore(bitmap, cancellationToken), cancellationToken);
    }

    public static Task<ScannerResult> ScanCollective(
        SKBitmap bitmap,
        IReadOnlyList<ZxingBarcodeScanStrategy> strategies,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ScanCollectiveCore(bitmap, strategies, cancellationToken), cancellationToken);
    }

    private ScannerResult ScanCore(SKBitmap bitmap, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var reader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = [Format]
                }
            };

            var result = reader.Decode(bitmap);
            var scanResult = result?.Text is { Length: > 0 } text
                ? ScannerResult.Success(text, Id, DisplayName, CodeType)
                : ScannerResult.Failure($"{DisplayName} did not find a readable code.");

            return scanResult;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return ScannerResult.Failure($"{DisplayName} failed while scanning.");
        }
    }

    private static ScannerResult ScanCollectiveCore(
        SKBitmap bitmap,
        IReadOnlyList<ZxingBarcodeScanStrategy> strategies,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var formats = strategies
                .Select(strategy => strategy.Format)
                .Distinct()
                .ToList();

            var reader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = formats
                }
            };

            var result = reader.Decode(bitmap);
            if (result?.Text is not { Length: > 0 } text)
            {
                return ScannerResult.Failure("ZXing did not find a readable code.");
            }

            var matchedStrategy = strategies.FirstOrDefault(strategy => strategy.Format == result.BarcodeFormat)
                ?? strategies[0];

            return ScannerResult.Success(
                text,
                matchedStrategy.Id,
                matchedStrategy.DisplayName,
                matchedStrategy.CodeType);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return ScannerResult.Failure("ZXing failed while scanning.");
        }
    }
}
