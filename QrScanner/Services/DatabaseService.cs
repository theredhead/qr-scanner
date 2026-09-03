using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QrScanner.Models;
using SQLite;

namespace QrScanner.Services;

public sealed class DatabaseService : IDatabaseService
{
    private readonly SQLiteAsyncConnection _connection;
    private readonly Task _initialization;

    public DatabaseService()
    {
        _connection = new SQLiteAsyncConnection(AppPaths.DatabasePath);
        _initialization = _connection.CreateTableAsync<ScanRecord>();
    }

    public async Task<int> InsertAsync(ScanRecord record)
    {
        await _initialization.ConfigureAwait(false);
        return await _connection.InsertAsync(record).ConfigureAwait(false);
    }

    public async Task<List<ScanRecord>> GetAllAsync()
    {
        await _initialization.ConfigureAwait(false);
        return await _connection.Table<ScanRecord>()
            .OrderByDescending(r => r.ScannedAtUtc)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<List<ScanRecord>> SearchAsync(string query)
    {
        await _initialization.ConfigureAwait(false);
        var like = $"%{query}%";
        return await _connection.QueryAsync<ScanRecord>(
            "SELECT * FROM ScanRecords WHERE RawText LIKE ? ORDER BY ScannedAtUtc DESC", like)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(ScanRecord record)
    {
        await _initialization.ConfigureAwait(false);
        await _connection.DeleteAsync(record).ConfigureAwait(false);
    }
}
