using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;

namespace ScannerZero.ViewModels;

public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly Action _onBack;

    public AboutViewModel(Action onBack)
    {
        _onBack = onBack;
    }

    public string Readme { get; } = ReadAsset("README.md");
    public string License { get; } = ReadAsset("LICENSE");
    public Uri RepositoryUri { get; } = new("https://github.com/theredhead/qr-scanner");
    public Uri LicenseUri { get; } = new("https://github.com/theredhead/qr-scanner/blob/main/LICENSE");

    [RelayCommand]
    private void Back() => _onBack();

    [RelayCommand]
    private async Task OpenRepositoryAsync(Avalonia.Controls.TopLevel? topLevel)
    {
        if (topLevel?.Launcher is not null)
            await topLevel.Launcher.LaunchUriAsync(RepositoryUri);
    }

    [RelayCommand]
    private async Task OpenLicenseAsync(Avalonia.Controls.TopLevel? topLevel)
    {
        if (topLevel?.Launcher is not null)
            await topLevel.Launcher.LaunchUriAsync(LicenseUri);
    }

    private static string ReadAsset(string name)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://ScannerZero/{name}"));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
