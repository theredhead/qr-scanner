using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ScannerZero.Services;

namespace ScannerZero.Desktop.Services;

public sealed class DesktopContactImporter : IContactImporter
{
    public Task<bool> ImportVCardAsync(string vCard)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "scannerzero-contact.vcf");
            File.WriteAllText(path, vCard, Encoding.UTF8);

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });

            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return Task.FromResult(false);
        }
    }
}
