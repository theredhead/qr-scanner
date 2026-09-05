using System;
using Avalonia;
using QrScanner.Desktop.Services;
using QrScanner.Services;

namespace QrScanner.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        PlatformServices.CameraFactory = () => new DesktopCameraScanService();

        if (args.Length > 0 && System.IO.File.Exists(args[0]))
        {
            try
            {
                var bytes = System.IO.File.ReadAllBytes(args[0]);
                ExternalImageHandler.HandleImage(bytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read CLI image argument: {ex}");
            }
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
