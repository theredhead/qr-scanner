using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ScannerZero.Services;

public sealed class ScannerStrategySettingsService
{
    public static ScannerStrategySettingsService Shared { get; } = new();

    private readonly object _gate = new();
    private readonly string _settingsPath;
    private ScannerStrategySettings? _settings;
    private IReadOnlyList<IScannerStrategy>? _enabledStrategies;

    public ScannerStrategySettingsService()
        : this(Path.Combine(AppPaths.DataDirectory, "scanner-strategies.json"))
    {
    }

    public ScannerStrategySettingsService(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public IReadOnlyList<ScannerStrategySetting> GetSettings()
    {
        lock (_gate)
        {
            return Clone(LoadSettings().Strategies);
        }
    }

    public IReadOnlyList<IScannerStrategy> GetOrderedEnabledStrategies()
    {
        lock (_gate)
        {
            if (_enabledStrategies is not null)
            {
                return _enabledStrategies;
            }

            var order = LoadSettings().Strategies
                .Where(s => s.IsEnabled)
                .ToDictionary(s => s.Id, s => s.SortOrder);

            _enabledStrategies = ScannerStrategies.All
                .Where(strategy => order.ContainsKey(strategy.Id))
                .OrderBy(strategy => order[strategy.Id])
                .ToList();

            return _enabledStrategies;
        }
    }

    public void Save(IReadOnlyList<ScannerStrategySetting> strategies)
    {
        lock (_gate)
        {
            _settings = Merge(strategies);
            _enabledStrategies = null;
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, JsonOptions));
        }
    }

    public void ResetToDefaults()
    {
        lock (_gate)
        {
            _settings = CreateDefaultSettings();
            _enabledStrategies = null;
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, JsonOptions));
        }
    }

    private ScannerStrategySettings LoadSettings()
    {
        if (_settings is not null)
        {
            return _settings;
        }

        try
        {
            if (File.Exists(_settingsPath))
            {
                var loaded = JsonSerializer.Deserialize<ScannerStrategySettings>(
                    File.ReadAllText(_settingsPath),
                    JsonOptions);

                _settings = Merge(loaded?.Strategies ?? []);
                return _settings;
            }
        }
        catch
        {
            // Fall back to defaults if the settings file is unreadable.
        }

        _settings = CreateDefaultSettings();
        return _settings;
    }

    private static ScannerStrategySettings Merge(IReadOnlyList<ScannerStrategySetting> saved)
    {
        var savedById = saved
            .GroupBy(s => s.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new ScannerStrategySettings();
        var nextOrder = 0;

        foreach (var strategy in ScannerStrategies.All)
        {
            if (savedById.TryGetValue(strategy.Id, out var setting))
            {
                result.Strategies.Add(new ScannerStrategySetting
                {
                    Id = strategy.Id,
                    IsEnabled = setting.IsEnabled,
                    SortOrder = setting.SortOrder
                });
            }
            else
            {
                result.Strategies.Add(new ScannerStrategySetting
                {
                    Id = strategy.Id,
                    IsEnabled = true,
                    SortOrder = nextOrder
                });
            }

            nextOrder++;
        }

        if (!result.Strategies.Any(s => s.IsEnabled) && result.Strategies.Count > 0)
        {
            result.Strategies[0].IsEnabled = true;
        }

        result.Strategies = result.Strategies
            .OrderBy(s => s.SortOrder)
            .Select((s, index) => new ScannerStrategySetting
            {
                Id = s.Id,
                IsEnabled = s.IsEnabled,
                SortOrder = index
            })
            .ToList();

        return result;
    }

    private static ScannerStrategySettings CreateDefaultSettings() => new()
    {
        Strategies = ScannerStrategies.All
            .Select((strategy, index) => new ScannerStrategySetting
            {
                Id = strategy.Id,
                IsEnabled = true,
                SortOrder = index
            })
            .ToList()
    };

    private static List<ScannerStrategySetting> Clone(IEnumerable<ScannerStrategySetting> strategies) =>
        strategies
            .Select(s => new ScannerStrategySetting
            {
                Id = s.Id,
                IsEnabled = s.IsEnabled,
                SortOrder = s.SortOrder
            })
            .ToList();

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true
    };
}
