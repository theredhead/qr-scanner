using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ScannerZero.ViewModels;
using ScannerZero.Views;

namespace ScannerZero;

/// <summary>
/// Strongly typed view locator with zero reflection overhead, safe for AOT and trimming.
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        return param switch
        {
            ScanViewModel => new ScanView(),
            ScanResultViewModel => new ScanResultView(),
            HistoryViewModel => new HistoryView(),
            SettingsViewModel => new SettingsView(),
            AboutViewModel => new AboutView(),
            ProcessingViewModel => new ProcessingView(),
            TextPayloadViewModel => new TextPayloadView(),
            UrlPayloadViewModel => new UrlPayloadView(),
            EmailPayloadViewModel => new EmailPayloadView(),
            PhonePayloadViewModel => new PhonePayloadView(),
            WifiPayloadViewModel => new WifiPayloadView(),
            VCardPayloadViewModel => new VCardPayloadView(),
            _ => param is null ? null : new TextBlock { Text = "Not Found: " + param.GetType().FullName }
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
