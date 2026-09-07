using System.Collections.Generic;

namespace QrScanner.Services;

public sealed class ScannerStrategySettings
{
    public List<ScannerStrategySetting> Strategies { get; set; } = [];
}

public sealed class ScannerStrategySetting
{
    public required string Id { get; set; }

    public bool IsEnabled { get; set; }

    public int SortOrder { get; set; }
}
