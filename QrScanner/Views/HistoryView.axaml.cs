using Avalonia.Controls;
using Avalonia.Interactivity;
using QrScanner.ViewModels;

namespace QrScanner.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }

    private void BackClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is HistoryViewModel history)
            history.SelectedRecord = null;
    }

    private async void ResetAllClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not HistoryViewModel history || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var confirm = new Window
        {
            Title = "Reset all data",
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = "Delete all scan history and saved images?", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 8,
                        Children =
                        {
                            new Button { Content = "Cancel" },
                            new Button { Content = "Reset all data", IsDefault = true }
                        }
                    }
                }
            }
        };

        var buttons = ((StackPanel)((StackPanel)confirm.Content!).Children[1]).Children;
        ((Button)buttons[0]).Click += (_, _) => confirm.Close(false);
        ((Button)buttons[1]).Click += (_, _) => confirm.Close(true);
        if (await confirm.ShowDialog<bool>(owner))
            await history.ResetAllAsync();
    }
}
