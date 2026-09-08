using System;
using System.IO;
using System.Linq;
using ScannerZero.Services;
using Xunit;

namespace ScannerZero.Tests;

public sealed class ScannerStrategySettingsServiceTests
{
    [Fact]
    public void GetSettings_returns_all_strategies_enabled_by_default()
    {
        var service = CreateService();

        var settings = service.GetSettings();

        Assert.Equal(ScannerStrategies.All.Count, settings.Count);
        Assert.All(settings, setting => Assert.True(setting.IsEnabled));
        Assert.Equal(Enumerable.Range(0, settings.Count), settings.Select(setting => setting.SortOrder));
    }

    [Fact]
    public void Save_preserves_order_and_enabled_state()
    {
        var service = CreateService();
        var reversed = service.GetSettings()
            .Reverse()
            .Select((setting, index) => new ScannerStrategySetting
            {
                Id = setting.Id,
                IsEnabled = index != 0,
                SortOrder = index
            })
            .ToList();

        service.Save(reversed);

        var saved = service.GetSettings();
        Assert.Equal(reversed.Select(setting => setting.Id), saved.Select(setting => setting.Id));
        Assert.False(saved[0].IsEnabled);
        Assert.Equal(Enumerable.Range(0, saved.Count), saved.Select(setting => setting.SortOrder));
    }

    [Fact]
    public void Save_reenables_first_strategy_when_all_are_disabled()
    {
        var service = CreateService();
        var disabled = service.GetSettings()
            .Select(setting => new ScannerStrategySetting
            {
                Id = setting.Id,
                IsEnabled = false,
                SortOrder = setting.SortOrder
            })
            .ToList();

        service.Save(disabled);

        var saved = service.GetSettings();
        Assert.True(saved[0].IsEnabled);
        Assert.Equal(1, saved.Count(setting => setting.IsEnabled));
    }

    [Fact]
    public void GetSettings_ignores_unknown_saved_strategies_and_adds_new_defaults()
    {
        var service = CreateService();
        var settings = service.GetSettings()
            .Take(2)
            .Select((setting, index) => new ScannerStrategySetting
            {
                Id = setting.Id,
                IsEnabled = index == 0,
                SortOrder = index + 1
            })
            .Append(new ScannerStrategySetting
            {
                Id = "mystery-format",
                IsEnabled = true,
                SortOrder = 0
            })
            .ToList();

        service.Save(settings);

        var saved = service.GetSettings();
        Assert.DoesNotContain(saved, setting => setting.Id == "mystery-format");
        Assert.Equal(ScannerStrategies.All.Count, saved.Count);
        Assert.True(saved.Single(setting => setting.Id == settings[0].Id).IsEnabled);
        Assert.True(saved.Where(setting => settings.All(s => s.Id != setting.Id)).All(setting => setting.IsEnabled));
    }

    [Fact]
    public void GetSettings_falls_back_to_defaults_when_file_is_unreadable()
    {
        var path = CreateSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ this is not json");
        var service = new ScannerStrategySettingsService(path);

        var settings = service.GetSettings();

        Assert.Equal(ScannerStrategies.All.Count, settings.Count);
        Assert.All(settings, setting => Assert.True(setting.IsEnabled));
    }

    [Fact]
    public void ResetToDefaults_restores_default_order_and_enabled_state()
    {
        var service = CreateService();
        var changed = service.GetSettings()
            .Reverse()
            .Select((setting, index) => new ScannerStrategySetting
            {
                Id = setting.Id,
                IsEnabled = index == 0,
                SortOrder = index
            })
            .ToList();
        service.Save(changed);

        service.ResetToDefaults();

        var settings = service.GetSettings();
        Assert.Equal(ScannerStrategies.All.Select(strategy => strategy.Id), settings.Select(setting => setting.Id));
        Assert.All(settings, setting => Assert.True(setting.IsEnabled));
    }

    [Fact]
    public void GetOrderedEnabledStrategies_uses_saved_order()
    {
        var service = CreateService();
        var firstThree = service.GetSettings().Take(3).Reverse().ToList();
        var remaining = service.GetSettings().Skip(3);
        service.Save(firstThree.Concat(remaining).Select((setting, index) => new ScannerStrategySetting
        {
            Id = setting.Id,
            IsEnabled = index < 3 && index != 1,
            SortOrder = index
        }).ToList());

        var enabled = service.GetOrderedEnabledStrategies();

        Assert.Equal(
            firstThree.Where((_, index) => index != 1).Select(setting => setting.Id),
            enabled.Select(strategy => strategy.Id));
    }

    private static ScannerStrategySettingsService CreateService()
    {
        return new ScannerStrategySettingsService(CreateSettingsPath());
    }

    private static string CreateSettingsPath()
    {
        var path = Path.Combine(Path.GetTempPath(), "ScannerZero.Tests", Guid.NewGuid().ToString("N"), "scanner-strategies.json");
        return path;
    }
}
