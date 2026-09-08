using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using AndroidX.Core.Content;
using QrScanner.Services;

namespace QrScanner.Android.Services;

public sealed class AndroidContactImporter(Activity activity) : IContactImporter
{
    public Task<bool> ImportVCardAsync(string vCard)
    {
        try
        {
            var file = new Java.IO.File(activity.FilesDir, "scannerzero-contact.vcf");
            File.WriteAllText(file.AbsolutePath, vCard, Encoding.UTF8);

            var uri = FileProvider.GetUriForFile(activity, $"{activity.PackageName}.fileprovider", file);
            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, "text/x-vcard");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.ClipData = ClipData.NewRawUri("ScannerZero contact", uri);

            activity.StartActivity(Intent.CreateChooser(intent, "Add contact"));
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is ActivityNotFoundException or IOException or Java.Lang.IllegalArgumentException)
        {
            return Task.FromResult(false);
        }
    }
}
