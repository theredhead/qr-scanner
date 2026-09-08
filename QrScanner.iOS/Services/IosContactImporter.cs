using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contacts;
using ContactsUI;
using Foundation;
using QrScanner.Services;
using UIKit;

namespace QrScanner.iOS.Services;

public sealed class IosContactImporter : IContactImporter
{
    public Task<bool> ImportVCardAsync(string vCard)
    {
        try
        {
            using var data = NSData.FromArray(Encoding.UTF8.GetBytes(vCard));
            var contacts = CNContactVCardSerialization.GetContacts(data, out var error);
            if (error is not null || contacts.Length == 0)
            {
                return Task.FromResult(false);
            }

            var mutableContact = contacts[0].MutableCopy() as CNMutableContact;
            if (mutableContact is null)
            {
                return Task.FromResult(false);
            }

            var controller = CNContactViewController.FromNewContact(mutableContact);
            var navigationController = new UINavigationController(controller);
            var presenter = UIApplication.SharedApplication.Windows.FirstOrDefault(window => window.IsKeyWindow)?.RootViewController;
            presenter?.PresentViewController(navigationController, true, null);

            return Task.FromResult(presenter is not null);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }
}
