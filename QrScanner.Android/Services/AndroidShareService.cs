using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using AndroidX.Core.Content;
using QrScanner.Services;

namespace QrScanner.Android.Services;

public sealed class AndroidShareService(Activity activity) : IShareService
{
    public Task ShareImageAsync(string imagePath)
    {
        var file = new Java.IO.File(imagePath);
        var uri = FileProvider.GetUriForFile(activity, $"{activity.PackageName}.fileprovider", file);
        var intent = new Intent(Intent.ActionSend);
        intent.SetType("image/jpeg");
        intent.PutExtra(Intent.ExtraStream, uri);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        activity.StartActivity(Intent.CreateChooser(intent, "Share QR image"));
        return Task.CompletedTask;
    }
}