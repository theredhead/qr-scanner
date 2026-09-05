using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using QrScanner.Services;

namespace QrScanner.iOS.Services;

public sealed class IosShareService : IShareService
{
    public Task ShareImageAsync(string imagePath)
    {
        using var image = UIImage.FromFile(imagePath);
        if (image is null)
            return Task.CompletedTask;

        var controller = new UIActivityViewController(
            new NSObject[] { image },
            null);
        var presenter = UIApplication.SharedApplication.Windows.FirstOrDefault(window => window.IsKeyWindow)?.RootViewController;
        presenter?.PresentViewController(controller, true, null);
        return Task.CompletedTask;
    }
}