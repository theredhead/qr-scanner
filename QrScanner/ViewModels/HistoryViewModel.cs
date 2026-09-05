using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IDatabaseService _db;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ScanRecord? SelectedRecord { get; set; }

    [ObservableProperty]
    public partial Bitmap? SelectedImage { get; set; }

    [ObservableProperty]
    public partial bool IsWifiSelected { get; set; }

    public bool IsWifiConnectSupported { get; } = PlatformServices.WifiConnectorFactory is not null;

    public bool IsShareSupported => PlatformServices.ShareFactory is not null;

    public bool IsDetailVisible => SelectedRecord is not null;

    public ObservableCollection<ScanRecord> Records { get; } = [];

    public HistoryViewModel(IDatabaseService db)
    {
        _db = db;
    }

    public async Task LoadAsync()
    {
        var items = string.IsNullOrWhiteSpace(SearchText)
            ? await _db.GetAllAsync().ConfigureAwait(true)
            : await _db.SearchAsync(SearchText).ConfigureAwait(true);

        Records.Clear();
        foreach (var item in items)
        {
            Records.Add(item);
        }
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    partial void OnSelectedRecordChanged(ScanRecord? value)
    {
        SelectedImage?.Dispose();
        SelectedImage = value is not null && File.Exists(value.ImagePath)
            ? new Bitmap(value.ImagePath)
            : null;
        IsWifiSelected = value is not null && QrContentParser.Parse(value.RawText).Wifi is not null;
        OnPropertyChanged(nameof(IsDetailVisible));
    }

    [RelayCommand]
    private async Task CopyTextAsync(TopLevel? topLevel)
    {
        if (SelectedRecord is not null && topLevel?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(SelectedRecord.RawText).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task OpenLinkAsync(TopLevel? topLevel)
    {
        if (SelectedRecord is null || topLevel?.Launcher is null)
        {
            return;
        }

        var parsed = QrContentParser.Parse(SelectedRecord.RawText);
        if (parsed.ActionUri is not null)
        {
            await topLevel.Launcher.LaunchUriAsync(new Uri(parsed.ActionUri)).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task ConnectWifiAsync()
    {
        if (SelectedRecord is null)
        {
            return;
        }

        var wifi = QrContentParser.Parse(SelectedRecord.RawText).Wifi;
        if (wifi is null)
        {
            return;
        }

        var connector = PlatformServices.WifiConnectorFactory?.Invoke();
        if (connector is not null)
        {
            await connector.ConnectAsync(wifi).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedRecord is not null)
        {
            await RemoveAsync(SelectedRecord).ConfigureAwait(true);
            SelectedRecord = null;
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(ScanRecord? record)
    {
        if (record is null)
            return;

        await _db.DeleteAsync(record).ConfigureAwait(true);
        if (File.Exists(record.ImagePath))
        {
            File.Delete(record.ImagePath);
        }

        await LoadAsync().ConfigureAwait(true);
    }

    public async Task ResetAllAsync()
    {
        var records = await _db.GetAllAsync().ConfigureAwait(true);
        await _db.DeleteAllAsync().ConfigureAwait(true);
        foreach (var record in records)
        {
            if (File.Exists(record.ImagePath))
                File.Delete(record.ImagePath);
        }

        SelectedRecord = null;
        await LoadAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (SelectedRecord is not null && File.Exists(SelectedRecord.ImagePath))
        {
            var share = PlatformServices.ShareFactory?.Invoke();
            if (share is not null)
                await share.ShareImageAsync(SelectedRecord.ImagePath).ConfigureAwait(true);
        }
    }
}
