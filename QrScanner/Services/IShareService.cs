using System.Threading.Tasks;

namespace QrScanner.Services;

public interface IShareService
{
    Task ShareImageAsync(string imagePath);
}