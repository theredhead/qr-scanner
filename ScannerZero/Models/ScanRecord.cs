using System;
using System.IO;
using ScannerZero.Services;
using SQLite;

namespace ScannerZero.Models;

[Table("ScanRecords")]
public sealed class ScanRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime ScannedAtUtc { get; set; }

    public string RawText { get; set; } = string.Empty;

    public ContentKind Kind { get; set; }

    public string StrategyId { get; set; } = "unknown";

    public string StrategyName { get; set; } = "Unknown strategy";

    public string CodeType { get; set; } = "Unknown";

    /// <summary>File name (not full path) of the saved snapshot image, relative to <see cref="AppPaths.ImagesDirectory"/>.</summary>
    public string ImageFileName { get; set; } = string.Empty;

    [Ignore]
    public string ImagePath => Path.Combine(AppPaths.ImagesDirectory, ImageFileName);
}
