
using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using QrScanner.Services;
using QrScanner.ViewModels;

namespace QrScanner.Views;

public partial class MainView : UserControl
{
    public static MainView? Current { get; private set; }

    public MainView()
    {
        InitializeComponent();
        Current = this;

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer is IAsyncDataTransfer asyncData)
        {
            var files = await asyncData.TryGetFilesAsync();
            if (files is not null)
            {
                foreach (var item in files)
                {
                    if (item is IStorageFile file)
                    {
                        using var stream = await file.OpenReadAsync();
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        var bytes = ms.ToArray();
                        if (bytes.Length > 0)
                        {
                            ExternalImageHandler.HandleImage(bytes);
                            break;
                        }
                    }
                }
            }
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    public bool TryNavigateBack() =>
        (DataContext as MainViewModel)?.TryNavigateBack() == true;
}