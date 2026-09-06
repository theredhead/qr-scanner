using Avalonia.Controls;
using QrScanner.ViewModels;

namespace QrScanner.Views;

public partial class ScanView : UserControl
{
    private bool _isAttached;

    public ScanView()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            _isAttached = true;
            StartCamera();
        };

        DetachedFromVisualTree += (_, _) =>
        {
            _isAttached = false;
            StopCamera();
        };

        DataContextChanged += (_, _) =>
        {
            if (_isAttached)
            {
                StartCamera();
            }
        };
    }

    private void StartCamera()
    {
        if (DataContext is ScanViewModel vm)
        {
            _ = vm.StartAsync();
        }
    }

    private void StopCamera()
    {
        if (DataContext is ScanViewModel vm)
        {
            _ = vm.StopAsync();
        }
    }
}
