using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScannerZero.Models;
using ScannerZero.Services;
using ScannerZero.ViewModels;
using Xunit;

namespace ScannerZero.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Constructor_loads_strategy_settings_in_saved_order()
    {
        var service = CreateSettingsService();
        var reversed = service.GetSettings()
            .Reverse()
            .Select((setting, index) => new ScannerStrategySetting
            {
                Id = setting.Id,
                IsEnabled = index != 1,
                SortOrder = index
            })
            .ToList();
        service.Save(reversed);

        var viewModel = CreateViewModel(service);

        Assert.Equal(reversed.Select(setting => setting.Id), viewModel.Strategies.Select(strategy => strategy.Id));
        Assert.False(viewModel.Strategies[1].IsEnabled);
        Assert.Equal($"{ScannerStrategies.All.Count - 1} strategies enabled", viewModel.EnabledStrategySummary);
    }

    [Fact]
    public void MoveStrategy_reorders_items_and_persists_new_order()
    {
        var service = CreateSettingsService();
        var viewModel = CreateViewModel(service);
        var last = viewModel.Strategies[^1];

        viewModel.MoveStrategy(last, 0);

        Assert.Same(last, viewModel.Strategies[0]);
        Assert.False(viewModel.Strategies[0].CanMoveUp);
        Assert.True(viewModel.Strategies[0].CanMoveDown);

        var reloaded = CreateViewModel(service);
        Assert.Equal(last.Id, reloaded.Strategies[0].Id);
    }

    [Fact]
    public void Last_enabled_strategy_cannot_be_disabled()
    {
        var viewModel = CreateViewModel(CreateSettingsService());

        foreach (var strategy in viewModel.Strategies.Skip(1))
        {
            strategy.IsEnabled = false;
        }

        var lastEnabled = viewModel.Strategies[0];
        Assert.True(lastEnabled.IsEnabled);
        Assert.False(lastEnabled.CanToggle);

        lastEnabled.IsEnabled = false;

        Assert.True(lastEnabled.IsEnabled);
        Assert.Equal("1 strategy enabled", viewModel.EnabledStrategySummary);
    }

    [Fact]
    public async Task ConfirmReset_resets_history_after_confirmation()
    {
        var historyStore = new FakeDatabaseService();
        var history = new HistoryViewModel(historyStore, _ => { });
        var viewModel = new SettingsViewModel(history, () => { }, CreateSettingsService());

        viewModel.RequestResetCommand.Execute(null);
        await viewModel.ConfirmResetCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsResetConfirmationVisible);
        Assert.Equal(1, historyStore.DeleteAllCallCount);
    }

    [Fact]
    public void ShowAbout_invokes_supplied_navigation_action()
    {
        var showAboutCount = 0;
        var viewModel = CreateViewModel(CreateSettingsService(), () => showAboutCount++);

        viewModel.ShowAboutCommand.Execute(null);

        Assert.Equal(1, showAboutCount);
    }

    private static SettingsViewModel CreateViewModel(
        ScannerStrategySettingsService service,
        Action? showAbout = null) =>
        new(new HistoryViewModel(new FakeDatabaseService(), _ => { }), showAbout ?? (() => { }), service);

    private static ScannerStrategySettingsService CreateSettingsService()
    {
        var path = Path.Combine(Path.GetTempPath(), "ScannerZero.Tests", Guid.NewGuid().ToString("N"), "scanner-strategies.json");
        return new ScannerStrategySettingsService(path);
    }

    private sealed class FakeDatabaseService : IDatabaseService
    {
        public int DeleteAllCallCount { get; private set; }

        public Task<int> InsertAsync(ScanRecord record) => Task.FromResult(1);

        public Task<List<ScanRecord>> GetAllAsync() => Task.FromResult(new List<ScanRecord>());

        public Task<List<ScanRecord>> SearchAsync(string query) => Task.FromResult(new List<ScanRecord>());

        public Task DeleteAsync(ScanRecord record) => Task.CompletedTask;

        public Task DeleteAllAsync()
        {
            DeleteAllCallCount++;
            return Task.CompletedTask;
        }
    }
}
