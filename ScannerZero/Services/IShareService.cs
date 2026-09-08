using System.Threading.Tasks;

namespace ScannerZero.Services;

public interface IShareService
{
    Task ShareImageAsync(string imagePath);
}