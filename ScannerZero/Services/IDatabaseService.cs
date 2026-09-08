using System.Collections.Generic;
using System.Threading.Tasks;
using ScannerZero.Models;

namespace ScannerZero.Services;

public interface IDatabaseService
{
    Task<int> InsertAsync(ScanRecord record);
    Task<List<ScanRecord>> GetAllAsync();
    Task<List<ScanRecord>> SearchAsync(string query);
    Task DeleteAsync(ScanRecord record);
    Task DeleteAllAsync();
}
