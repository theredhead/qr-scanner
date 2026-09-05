using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    public ScanViewModel Scan { get; }

    public HistoryViewModel History { get; }

    public AboutViewModel About { get; }

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial bool IsAboutVisible { get; set; }

    public MainViewModel()
    {
        var db = new DatabaseService();
        Scan = new ScanViewModel(db);
        History = new HistoryViewModel(db);
        About = new AboutViewModel(History);

        ExternalImageHandler.RegisterReceiver(ProcessSharedImageAsync);
        UpdateCameraState();
    }

    public async Task ProcessSharedImageAsync(byte[] imageBytes)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsAboutVisible = false;
            SelectedTabIndex = 0;
        });

        await Scan.ScanImageAsync(imageBytes).ConfigureAwait(false);
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

    private void UpdateCameraState()
    {
        if (SelectedTabIndex == 0 && !IsAboutVisible)
        {
            _ = Scan.StartAsync();
        }
        else
        {
            _ = Scan.StopAsync();
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ShowAbout() => IsAboutVisible = true;

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void HideAbout() => IsAboutVisible = false;

    public bool TryNavigateBack()
    {
        if (IsAboutVisible)
        {
            IsAboutVisible = false;
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
        Scan.Dispose();
    }
}
