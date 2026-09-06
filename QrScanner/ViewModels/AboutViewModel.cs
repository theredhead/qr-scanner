using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly HistoryViewModel _history;
    private readonly Action _onBack;

    public AboutViewModel(HistoryViewModel history, Action onBack)
    {
        _history = history;
        _onBack = onBack;
    }

    public string Readme { get; } = ReadAsset("README.md");
    public string License { get; } = ReadAsset("LICENSE");
    public Uri RepositoryUri { get; } = new("https://github.com/theredhead/qr-scanner");
    public Uri LicenseUri { get; } = new("https://github.com/theredhead/qr-scanner/blob/main/LICENSE");

    [ObservableProperty]
    public partial bool IsResetConfirmationVisible { get; set; }

    [RelayCommand]
    private void Back() => _onBack();

    [RelayCommand]
    private void RequestReset() => IsResetConfirmationVisible = true;

    [RelayCommand]
    private void CancelReset() => IsResetConfirmationVisible = false;

    [RelayCommand]
    private async Task ConfirmResetAsync()
    {
        IsResetConfirmationVisible = false;
        await _history.ResetAllAsync();
    }

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
        using var stream = AssetLoader.Open(new Uri($"avares://QrScanner/{name}"));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
