using ScannerZero.Services;
using Xunit;

namespace ScannerZero.Tests;

public sealed class ScannerResultTests
{
    [Fact]
    public void Success_populates_scan_metadata()
    {
        var result = ScannerResult.Success("ScannerZero", "qr-code", "QR Code", "QR Code");

        Assert.True(result.IsSuccess);
        Assert.Equal("ScannerZero", result.RawText);
        Assert.Equal("qr-code", result.StrategyId);
        Assert.Equal("QR Code", result.StrategyName);
        Assert.Equal("QR Code", result.CodeType);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_populates_error_message_only()
    {
        var result = ScannerResult.Failure("No code found");

        Assert.False(result.IsSuccess);
        Assert.Equal("No code found", result.ErrorMessage);
        Assert.Null(result.RawText);
        Assert.Null(result.StrategyId);
        Assert.Null(result.StrategyName);
        Assert.Null(result.CodeType);
    }
}
