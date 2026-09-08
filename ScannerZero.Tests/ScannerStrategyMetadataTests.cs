using System.Linq;
using ScannerZero.Services;
using Xunit;

namespace ScannerZero.Tests;

public sealed class ScannerStrategyMetadataTests
{
    [Fact]
    public void All_strategies_have_unique_ids_and_display_metadata()
    {
        var strategies = ScannerStrategies.All;

        Assert.NotEmpty(strategies);
        Assert.Equal(strategies.Count, strategies.Select(strategy => strategy.Id).Distinct().Count());
        Assert.All(strategies, strategy =>
        {
            Assert.False(string.IsNullOrWhiteSpace(strategy.Id));
            Assert.False(string.IsNullOrWhiteSpace(strategy.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(strategy.CodeType));
        });
    }
}
