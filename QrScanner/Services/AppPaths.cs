using System;
using System.IO;

namespace QrScanner.Services;

/// <summary>Locations for the app's local database and saved scan images.</summary>
public static class AppPaths
{
    public static string DataDirectory { get; } = EnsureDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QrScanner"));

    public static string ImagesDirectory { get; } = EnsureDirectory(Path.Combine(DataDirectory, "images"));

    public static string DatabasePath => Path.Combine(DataDirectory, "qrscanner.db3");

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
