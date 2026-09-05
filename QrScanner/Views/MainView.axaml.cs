
using Avalonia;
using Avalonia.Controls;
using QrScanner.ViewModels;

namespace QrScanner.Views;

public partial class MainView : UserControl
{
    public static MainView? Current { get; private set; }

    public MainView()
    {
        InitializeComponent();
        Current = this;
    }

    public bool TryNavigateBack() =>
        (DataContext as MainViewModel)?.TryNavigateBack() == true;
}