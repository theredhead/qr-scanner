using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrScanner.Models;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IDatabaseService _db;
    private readonly Action<ScanRecord> _onRecordSelected;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    public ObservableCollection<ScanRecord> Records { get; } = [];

    public HistoryViewModel(IDatabaseService db, Action<ScanRecord> onRecordSelected)
    {
        _db = db;
        _onRecordSelected = onRecordSelected;
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

    [RelayCommand]
    private void OpenRecord(ScanRecord? record)
    {
        if (record is not null)
            _onRecordSelected(record);
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

        await LoadAsync().ConfigureAwait(true);
    }
}
