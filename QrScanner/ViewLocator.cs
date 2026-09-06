using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using QrScanner.ViewModels;
using QrScanner.Views;

namespace QrScanner;

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
            AboutViewModel => new AboutView(),
            _ => param is null ? null : new TextBlock { Text = "Not Found: " + param.GetType().FullName }
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}