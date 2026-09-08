using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using SkiaSharp;

namespace ScannerZero.Services;

public static class ScannerStrategyRunner
{
    public static async Task<ScannerResult> Scan(SKBitmap bitmap, CancellationToken cancellationToken = default)
    {
        var strategies = ScannerStrategySettingsService.Shared.GetOrderedEnabledStrategies();
        if (strategies.Count == 0)
        {
            return ScannerResult.Failure("No scanner strategies are enabled.");
        }

        if (strategies.All(strategy => strategy is ZxingBarcodeScanStrategy))
        {
            var zxingStrategies = strategies.Cast<ZxingBarcodeScanStrategy>().ToList();
            return await ZxingBarcodeScanStrategy.ScanCollective(bitmap, zxingStrategies, cancellationToken).ConfigureAwait(false);
        }

        foreach (var strategy in strategies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await strategy.Scan(bitmap, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return result;
            }
        }

        return ScannerResult.Failure("No enabled barcode strategy could read this image.");
    }
}
