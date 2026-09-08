using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace ScannerZero.Services;

public interface IScannerStrategy
{
    string Id { get; }

    string DisplayName { get; }

    string CodeType { get; }

    Task<ScannerResult> Scan(SKBitmap bitmap, CancellationToken cancellationToken = default);
}
