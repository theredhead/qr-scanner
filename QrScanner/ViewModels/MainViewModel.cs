using System;
using CommunityToolkit.Mvvm.ComponentModel;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    public ScanViewModel Scan { get; }

    public HistoryViewModel History { get; }

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    public MainViewModel()
    {
        var db = new DatabaseService();
        Scan = new ScanViewModel(db);
        History = new HistoryViewModel(db);

        // The Scan tab is selected by default, so kick off the camera without waiting for a tab change.
        _ = Scan.StartAsync();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == 0)
        {
            _ = Scan.StartAsync();
        }
        else
        {
            _ = Scan.StopAsync();
        }

        if (value == 1)
        {
            _ = History.LoadAsync();
        }
    }

    public void Dispose() => Scan.Dispose();
}
