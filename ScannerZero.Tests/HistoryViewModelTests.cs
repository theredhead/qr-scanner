using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScannerZero.Models;
using ScannerZero.Services;
using ScannerZero.ViewModels;
using Xunit;

namespace ScannerZero.Tests;

public sealed class HistoryViewModelTests
{
    [Fact]
    public async Task LoadAsync_loads_all_records_when_search_is_blank()
    {
        var records = new[]
        {
            CreateRecord("ScannerZero"),
            CreateRecord("Pi")
        };
        var db = new FakeDatabaseService { AllRecords = records.ToList() };
        var viewModel = new HistoryViewModel(db, _ => { });

        await viewModel.LoadAsync();

        Assert.Equal(records.Select(record => record.RawText), viewModel.Records.Select(record => record.RawText));
        Assert.Equal(1, db.GetAllCallCount);
        Assert.Equal(0, db.SearchCallCount);
    }

    [Fact]
    public async Task LoadAsync_uses_search_when_search_text_is_present()
    {
        var db = new FakeDatabaseService { SearchRecords = [CreateRecord("3.14159")] };
        var viewModel = new HistoryViewModel(db, _ => { })
        {
            SearchText = "pi"
        };

        await viewModel.LoadAsync();

        Assert.Equal("pi", db.LastSearchQuery);
        Assert.Equal("3.14159", Assert.Single(viewModel.Records).RawText);
    }

    [Fact]
    public void OpenRecord_invokes_selection_callback()
    {
        var record = CreateRecord("ScannerZero");
        ScanRecord? selected = null;
        var viewModel = new HistoryViewModel(new FakeDatabaseService(), selectedRecord => selected = selectedRecord);

        viewModel.OpenRecordCommand.Execute(record);

        Assert.Same(record, selected);
    }

    [Fact]
    public async Task Remove_deletes_record_image_and_reloads()
    {
        var record = CreateRecord("ScannerZero", "missing-scan.jpg");
        var db = new FakeDatabaseService();
        var viewModel = new HistoryViewModel(db, _ => { });

        await viewModel.RemoveCommand.ExecuteAsync(record);

        Assert.Same(record, db.DeletedRecord);
        Assert.Equal(1, db.GetAllCallCount);
    }

    [Fact]
    public async Task ResetAll_deletes_all_records_and_images()
    {
        var db = new FakeDatabaseService { AllRecords = [CreateRecord("ScannerZero", "missing-scan.jpg")] };
        var viewModel = new HistoryViewModel(db, _ => { });

        await viewModel.ResetAllAsync();

        Assert.Equal(1, db.DeleteAllCallCount);
    }

    private static ScanRecord CreateRecord(string rawText, string imageFileName = "") => new()
    {
        ScannedAtUtc = DateTime.UtcNow,
        RawText = rawText,
        Kind = ContentKind.Text,
        StrategyId = "qr-code",
        StrategyName = "QR Code",
        CodeType = "QR Code",
        ImageFileName = imageFileName
    };

    private sealed class FakeDatabaseService : IDatabaseService
    {
        public List<ScanRecord> AllRecords { get; init; } = [];
        public List<ScanRecord> SearchRecords { get; init; } = [];
        public int GetAllCallCount { get; private set; }
        public int SearchCallCount { get; private set; }
        public int DeleteAllCallCount { get; private set; }
        public string? LastSearchQuery { get; private set; }
        public ScanRecord? DeletedRecord { get; private set; }

        public Task<int> InsertAsync(ScanRecord record) => Task.FromResult(1);

        public Task<List<ScanRecord>> GetAllAsync()
        {
            GetAllCallCount++;
            return Task.FromResult(AllRecords);
        }

        public Task<List<ScanRecord>> SearchAsync(string query)
        {
            SearchCallCount++;
            LastSearchQuery = query;
            return Task.FromResult(SearchRecords);
        }

        public Task DeleteAsync(ScanRecord record)
        {
            DeletedRecord = record;
            return Task.CompletedTask;
        }

        public Task DeleteAllAsync()
        {
            DeleteAllCallCount++;
            return Task.CompletedTask;
        }
    }
}
