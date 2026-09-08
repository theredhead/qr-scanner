using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ScannerZero.ViewModels;

namespace ScannerZero.Views;

public partial class SettingsView : UserControl
{
    private const double DragThreshold = 8;
    private ScannerStrategyItemViewModel? _draggedStrategy;
    private IPointer? _dragPointer;
    private Point _dragOffset;
    private Point _dragStart;
    private bool _isDragging;

    public SettingsView()
    {
        InitializeComponent();
        AddHandler(PointerMovedEvent, StrategyDragHandle_PointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, StrategyDragHandle_PointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, StrategyDragHandle_PointerCaptureLost, RoutingStrategies.Tunnel);
    }

    private void StrategyDragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control handle ||
            handle.DataContext is not ScannerStrategyItemViewModel strategy)
        {
            return;
        }

        _dragPointer = e.Pointer;
        _draggedStrategy = strategy;
        _dragStart = e.GetPosition(this);
        _isDragging = false;
        ShowDragGhost(strategy, _dragStart);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void StrategyDragHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedStrategy is null ||
            _dragPointer != e.Pointer ||
            DataContext is not SettingsViewModel settings)
        {
            return;
        }

        var position = e.GetPosition(this);
        PositionDragGhost(position);

        if (!_isDragging && Distance(position, _dragStart) < DragThreshold)
        {
            e.Handled = true;
            return;
        }

        _isDragging = true;

        var targetIndex = FindTargetIndex(position, settings);
        if (targetIndex < 0 || targetIndex == settings.Strategies.IndexOf(_draggedStrategy))
        {
            e.Handled = true;
            return;
        }

        settings.MoveStrategy(_draggedStrategy, targetIndex);
        e.Handled = true;
    }

    private void StrategyDragHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggedStrategy is null || _dragPointer != e.Pointer)
        {
            return;
        }

        EndDrag();
        e.Handled = true;
    }

    private void StrategyDragHandle_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => EndDrag();

    private int FindTargetIndex(Point pointerPosition, SettingsViewModel settings)
    {
        var rows = GetStrategyRows();
        if (rows.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].TranslatePoint(new Point(0, rows[i].Bounds.Height / 2), this) is { } midpoint &&
                pointerPosition.Y < midpoint.Y)
            {
                return rows[i].DataContext is ScannerStrategyItemViewModel target
                    ? settings.Strategies.IndexOf(target)
                    : i;
            }
        }

        return settings.Strategies.Count - 1;
    }

    private List<Border> GetStrategyRows()
    {
        return StrategyItems.GetVisualDescendants()
            .OfType<Border>()
            .Where(row => row.Classes.Contains("strategy-row"))
            .ToList();
    }

    private void ShowDragGhost(ScannerStrategyItemViewModel strategy, Point pointerPosition)
    {
        var row = GetStrategyRows()
            .FirstOrDefault(item => ReferenceEquals(item.DataContext, strategy));
        var rowOrigin = row?.TranslatePoint(new Point(0, 0), this) ?? new Point(20, pointerPosition.Y - 32);
        var rowWidth = row?.Bounds.Width ?? Math.Max(240, Bounds.Width - 40);

        DragGhostTitle.Text = strategy.DisplayName;
        DragGhostSubtitle.Text = strategy.CodeType;
        DragGhost.Width = Math.Max(240, Math.Min(rowWidth, Bounds.Width - 40));
        DragGhost.IsVisible = true;

        _dragOffset = new Point(
            pointerPosition.X - rowOrigin.X,
            pointerPosition.Y - rowOrigin.Y);
        PositionDragGhost(pointerPosition);
    }

    private void PositionDragGhost(Point pointerPosition)
    {
        if (!DragGhost.IsVisible)
        {
            return;
        }

        var maxLeft = Math.Max(12, Bounds.Width - DragGhost.Width - 12);
        Canvas.SetLeft(DragGhost, Math.Clamp(pointerPosition.X - _dragOffset.X, 12, maxLeft));
        Canvas.SetTop(DragGhost, pointerPosition.Y - _dragOffset.Y);
    }

    private void EndDrag()
    {
        _dragPointer?.Capture(null);
        _dragPointer = null;
        _draggedStrategy = null;
        DragGhost.IsVisible = false;
        _isDragging = false;
    }

    private static double Distance(Point first, Point second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
    }
}
