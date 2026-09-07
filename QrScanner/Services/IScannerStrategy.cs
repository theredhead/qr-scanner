using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace QrScanner.Services;

public interface IScannerStrategy
{
    string Id { get; }

    string DisplayName { get; }

    string CodeType { get; }

    Task<ScannerResult> Scan(SKBitmap bitmap, CancellationToken cancellationToken = default);
}
