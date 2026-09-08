using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScannerZero.Models;
using ScannerZero.Services;

namespace ScannerZero.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IDatabaseService _db;

    public ScanViewModel Scan { get; }
    public HistoryViewModel History { get; }
    public SettingsViewModel Settings { get; }
    public AboutViewModel About { get; }

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public bool IsScanActive => CurrentPage is ScanViewModel;
    public bool IsHistoryActive => CurrentPage is HistoryViewModel;
    public bool IsSettingsActive => CurrentPage is SettingsViewModel;
    public bool IsNavBarVisible => CurrentPage is ScanViewModel or HistoryViewModel or SettingsViewModel or AboutViewModel;

    public MainViewModel()
    {
        _db = new DatabaseService();
        Scan = new ScanViewModel(_db, OnLiveScanCompleted);
        History = new HistoryViewModel(_db, OnHistoryRecordSelected);
        Settings = new SettingsViewModel(History, NavigateToAbout);
        About = new AboutViewModel(NavigateToSettings);

        if (ExternalImageHandler.IsIngesting)
        {
            _currentPage = new ProcessingViewModel("Scanning shared image...");
        }
        else
        {
            _currentPage = Scan;
        }

        ExternalImageHandler.RegisterReceiver(ProcessSharedImageAsync);
    }

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(IsScanActive));
        OnPropertyChanged(nameof(IsHistoryActive));
        OnPropertyChanged(nameof(IsSettingsActive));
        OnPropertyChanged(nameof(IsNavBarVisible));

        if (value is ScanViewModel)
        {
            _ = Scan.StartAsync();
        }
        else
        {
            _ = Scan.StopAsync();
        }

        if (value is HistoryViewModel)
        {
            _ = History.LoadAsync();
        }
    }

    [RelayCommand]
    public void NavigateToScan()
    {
        if (CurrentPage is ScanViewModel)
        {
            _ = Scan.StartAsync();
        }
        else
        {
            CurrentPage = Scan;
        }
    }

    [RelayCommand]
    public void NavigateToHistory() => CurrentPage = History;

    [RelayCommand]
    public void NavigateToSettings() => CurrentPage = Settings;

    [RelayCommand]
    public void NavigateToAbout() => CurrentPage = About;

    public void ActivateCurrentPage()
    {
        if (CurrentPage is ScanViewModel)
        {
            _ = Scan.StartAsync();
        }
    }

    public void PrepareForExternalImageIntent()
    {
        Scan.PrepareForExternalImageIntent();
    }

    private void OnLiveScanCompleted(ScanRecord record, byte[] jpegBytes)
    {
        CurrentPage = ScanResultViewModel.CreateSuccess(
            record.RawText,
            jpegBytes,
            record.ImagePath,
            record.StrategyName,
            record.CodeType,
            onDismiss: NavigateToScan);
    }

    private void OnHistoryRecordSelected(ScanRecord record)
    {
        byte[]? jpegBytes = null;
        if (File.Exists(record.ImagePath))
        {
            try { jpegBytes = File.ReadAllBytes(record.ImagePath); } catch { }
        }

        CurrentPage = ScanResultViewModel.CreateSuccess(
            record.RawText,
            jpegBytes ?? [],
            record.ImagePath,
            record.StrategyName,
            record.CodeType,
            onDismiss: NavigateToHistory);
    }

    public async Task ProcessSharedImageAsync(byte[] imageBytes)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _ = Scan.StopAsync();
            CurrentPage = new ProcessingViewModel("Scanning shared image...");
        });

        var (scanResult, jpegBytes) = await BarcodeDecoder.ScanImageBytes(imageBytes).ConfigureAwait(false);

        if (scanResult.IsSuccess && !string.IsNullOrEmpty(scanResult.RawText) && jpegBytes is not null)
        {
            var fileName = $"{Guid.NewGuid():N}.jpg";
            var path = Path.Combine(AppPaths.ImagesDirectory, fileName);
            await File.WriteAllBytesAsync(path, jpegBytes).ConfigureAwait(false);

            var parsed = ScanPayloadParser.Parse(scanResult.RawText);
            var record = new ScanRecord
            {
                ScannedAtUtc = DateTime.UtcNow,
                RawText = scanResult.RawText,
                Kind = parsed.Kind,
                StrategyId = scanResult.StrategyId ?? "unknown",
                StrategyName = scanResult.StrategyName ?? "Unknown strategy",
                CodeType = scanResult.CodeType ?? "Unknown",
                ImageFileName = fileName
            };
            await _db.InsertAsync(record).ConfigureAwait(false);

            Dispatcher.UIThread.Post(() =>
            {
                CurrentPage = ScanResultViewModel.CreateSuccess(
                    scanResult.RawText,
                    jpegBytes,
                    path,
                    record.StrategyName,
                    record.CodeType,
                    onDismiss: NavigateToScan);
            });
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                CurrentPage = ScanResultViewModel.CreateFailure(
                    imageBytes,
                    "No enabled barcode found in shared image",
                    onDismiss: NavigateToScan);
            });
        }
    }

    public bool TryNavigateBack()
    {
        if (CurrentPage is AboutViewModel)
        {
            NavigateToSettings();
            return true;
        }

        if (CurrentPage is ScanResultViewModel or SettingsViewModel or ProcessingViewModel)
        {
            NavigateToScan();
            return true;
        }

        if (CurrentPage is HistoryViewModel)
        {
            NavigateToScan();
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        ExternalImageHandler.UnregisterReceiver(ProcessSharedImageAsync);
        Scan.Dispose();
    }
}
