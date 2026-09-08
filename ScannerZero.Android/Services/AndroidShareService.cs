using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using AndroidX.Core.Content;
using ScannerZero.Services;

namespace ScannerZero.Android.Services;

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
        activity.StartActivity(Intent.CreateChooser(intent, "Share code image"));
        return Task.CompletedTask;
    }
}