using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrScanner.Services;

namespace QrScanner.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ScannerStrategySettingsService _settings = ScannerStrategySettingsService.Shared;
    private readonly HistoryViewModel _history;
    private readonly Action _showAbout;

    public ObservableCollection<ScannerStrategyItemViewModel> Strategies { get; } = [];

    [ObservableProperty]
    public partial bool IsResetConfirmationVisible { get; set; }

    public string EnabledStrategySummary
    {
        get
        {
            var count = Strategies.Count(s => s.IsEnabled);
            return count == 1 ? "1 strategy enabled" : $"{count} strategies enabled";
        }
    }

    public SettingsViewModel(HistoryViewModel history, Action showAbout)
    {
        _history = history;
        _showAbout = showAbout;
        Load();
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        _settings.ResetToDefaults();
        Load();
    }

    [RelayCommand]
    private void ShowAbout() => _showAbout();

    [RelayCommand]
    private void RequestReset() => IsResetConfirmationVisible = true;

    [RelayCommand]
    private void CancelReset() => IsResetConfirmationVisible = false;

    [RelayCommand]
    private async System.Threading.Tasks.Task ConfirmResetAsync()
    {
        IsResetConfirmationVisible = false;
        await _history.ResetAllAsync();
    }

    private void Load()
    {
        Strategies.Clear();

        var definitions = ScannerStrategies.All.ToDictionary(s => s.Id);
        foreach (var setting in _settings.GetSettings().OrderBy(s => s.SortOrder))
        {
            if (definitions.TryGetValue(setting.Id, out var strategy))
            {
                Strategies.Add(new ScannerStrategyItemViewModel(
                    strategy.Id,
                    strategy.DisplayName,
                    strategy.CodeType,
                    setting.IsEnabled,
                    CanDisable,
                    MoveUp,
                    MoveDown,
                    Save));
            }
        }

        RefreshOrderingState();
    }

    private void MoveUp(ScannerStrategyItemViewModel item)
    {
        var index = Strategies.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        Strategies.Move(index, index - 1);
        Save();
    }

    private void MoveDown(ScannerStrategyItemViewModel item)
    {
        var index = Strategies.IndexOf(item);
        if (index < 0 || index >= Strategies.Count - 1)
        {
            return;
        }

        Strategies.Move(index, index + 1);
        Save();
    }

    private void Save()
    {
        _settings.Save(Strategies.Select((item, index) => new ScannerStrategySetting
        {
            Id = item.Id,
            IsEnabled = item.IsEnabled,
            SortOrder = index
        }).ToList());

        RefreshOrderingState();
        OnPropertyChanged(nameof(EnabledStrategySummary));
    }

    private void RefreshOrderingState()
    {
        for (var i = 0; i < Strategies.Count; i++)
        {
            Strategies[i].CanMoveUp = i > 0;
            Strategies[i].CanMoveDown = i < Strategies.Count - 1;
            Strategies[i].CanToggle = !Strategies[i].IsEnabled || CanDisable(Strategies[i]);
        }
    }

    private bool CanDisable(ScannerStrategyItemViewModel item) =>
        Strategies.Any(s => !ReferenceEquals(s, item) && s.IsEnabled);
}

public sealed partial class ScannerStrategyItemViewModel : ViewModelBase
{
    private readonly Action<ScannerStrategyItemViewModel> _moveUp;
    private readonly Action<ScannerStrategyItemViewModel> _moveDown;
    private readonly Func<ScannerStrategyItemViewModel, bool> _canDisable;
    private readonly Action _changed;
    private bool _isInitialized;

    public ScannerStrategyItemViewModel(
        string id,
        string displayName,
        string codeType,
        bool isEnabled,
        Func<ScannerStrategyItemViewModel, bool> canDisable,
        Action<ScannerStrategyItemViewModel> moveUp,
        Action<ScannerStrategyItemViewModel> moveDown,
        Action changed)
    {
        Id = id;
        DisplayName = displayName;
        CodeType = codeType;
        _canDisable = canDisable;
        _moveUp = moveUp;
        _moveDown = moveDown;
        _changed = changed;
        IsEnabled = isEnabled;
        _isInitialized = true;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string CodeType { get; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool CanMoveUp { get; set; }

    [ObservableProperty]
    public partial bool CanMoveDown { get; set; }

    [ObservableProperty]
    public partial bool CanToggle { get; set; }

    partial void OnIsEnabledChanged(bool value)
    {
        if (!_isInitialized)
        {
            return;
        }

        if (!value && !_canDisable(this))
        {
            IsEnabled = true;
            return;
        }

        _changed();
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => _moveUp(this);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => _moveDown(this);

    partial void OnCanMoveUpChanged(bool value) => MoveUpCommand.NotifyCanExecuteChanged();

    partial void OnCanMoveDownChanged(bool value) => MoveDownCommand.NotifyCanExecuteChanged();
}
