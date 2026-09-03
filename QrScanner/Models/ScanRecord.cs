using System;
using System.IO;
using QrScanner.Services;
using SQLite;

namespace QrScanner.Models;

[Table("ScanRecords")]
public sealed class ScanRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime ScannedAtUtc { get; set; }

    public string RawText { get; set; } = string.Empty;

    public ContentKind Kind { get; set; }

    /// <summary>File name (not full path) of the saved snapshot image, relative to <see cref="AppPaths.ImagesDirectory"/>.</summary>
    public string ImageFileName { get; set; } = string.Empty;

    [Ignore]
    public string ImagePath => Path.Combine(AppPaths.ImagesDirectory, ImageFileName);
}
