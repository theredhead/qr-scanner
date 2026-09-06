using CommunityToolkit.Mvvm.ComponentModel;

namespace QrScanner.ViewModels;

public sealed partial class ProcessingViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message;

    public ProcessingViewModel(string message = "Scanning shared image...")
    {
        _message = message;
    }
}
