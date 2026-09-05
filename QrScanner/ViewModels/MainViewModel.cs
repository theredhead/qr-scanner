using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IDatabaseService _db;

    public ScanViewModel Scan { get; }
    public HistoryViewModel History { get; }
    public AboutViewModel About { get; }

    [ObservableProperty]
    public partial ScanResultViewModel? CurrentResult { get; set; }

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial bool IsAboutVisible { get; set; }

    public MainViewModel()
    {
        _db = new DatabaseService();
        Scan = new ScanViewModel(_db, OnLiveScanCompleted);
        History = new HistoryViewModel(_db);
        About = new AboutViewModel(History);

        ExternalImageHandler.RegisterReceiver(ProcessSharedImageAsync);

        if (!ExternalImageHandler.HasPendingImages)
        {
            UpdateCameraState();
        }
    }

    private void OnLiveScanCompleted(ScanRecord record, byte[] jpegBytes)
    {
        CurrentResult?.Dispose();
        CurrentResult = ScanResultViewModel.CreateSuccess(
            record.RawText,
            jpegBytes,
            record.ImagePath,
            onDismiss: CloseResult);
        UpdateCameraState();
    }

    public async Task ProcessSharedImageAsync(byte[] imageBytes)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsAboutVisible = false;
            SelectedTabIndex = 0;
            _ = Scan.StopAsync();
        });

        var (rawText, jpegBytes) = await Task.Run(() => QrDecoder.DecodeImageBytes(imageBytes)).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(rawText) && jpegBytes is not null)
        {
            var fileName = $"{Guid.NewGuid():N}.jpg";
            var path = Path.Combine(AppPaths.ImagesDirectory, fileName);
            await File.WriteAllBytesAsync(path, jpegBytes).ConfigureAwait(false);

            var parsed = QrContentParser.Parse(rawText);
            var record = new ScanRecord
            {
                ScannedAtUtc = DateTime.UtcNow,
                RawText = rawText,
                Kind = parsed.Kind,
                ImageFileName = fileName
            };
            await _db.InsertAsync(record).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentResult?.Dispose();
                CurrentResult = ScanResultViewModel.CreateSuccess(
                    rawText,
                    jpegBytes,
                    path,
                    onDismiss: CloseResult);
                UpdateCameraState();
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentResult?.Dispose();
                CurrentResult = ScanResultViewModel.CreateFailure(
                    imageBytes,
                    "No QR code found in shared image",
                    onDismiss: CloseResult);
                UpdateCameraState();
            });
        }
    }

    private void CloseResult()
    {
        CurrentResult?.Dispose();
        CurrentResult = null;
        UpdateCameraState();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (IsAboutVisible)
            IsAboutVisible = false;

        UpdateCameraState();

        if (value == 1)
        {
            _ = History.LoadAsync();
        }
    }

    partial void OnIsAboutVisibleChanged(bool value)
    {
        UpdateCameraState();
    }

    partial void OnCurrentResultChanged(ScanResultViewModel? value)
    {
        UpdateCameraState();
    }

    private void UpdateCameraState()
    {
        if (SelectedTabIndex == 0 && !IsAboutVisible && CurrentResult == null && !ExternalImageHandler.HasPendingImages)
        {
            _ = Scan.StartAsync();
        }
        else
        {
            _ = Scan.StopAsync();
        }
    }

    [RelayCommand]
    private void ShowAbout() => IsAboutVisible = true;

    [RelayCommand]
    private void HideAbout() => IsAboutVisible = false;

    public bool TryNavigateBack()
    {
        if (IsAboutVisible)
        {
            IsAboutVisible = false;
            return true;
        }

        if (CurrentResult is not null)
        {
            CloseResult();
            return true;
        }

        if (History.SelectedRecord is not null)
        {
            History.SelectedRecord = null;
            return true;
        }

        if (SelectedTabIndex != 0)
        {
            SelectedTabIndex = 0;
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        ExternalImageHandler.UnregisterReceiver(ProcessSharedImageAsync);
        CurrentResult?.Dispose();
        Scan.Dispose();
    }
}
