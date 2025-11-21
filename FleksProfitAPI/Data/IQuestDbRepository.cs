using FleksProfitAPI.Models;

namespace FleksProfitAPI.Data
{
    public interface IQuestDbRepository
    {
        Task EnsureTableExistsAsync(CancellationToken ct = default);
        Task<int> InsertFcrRecordsAsync(IEnumerable<FcrRecord> records, CancellationToken ct = default);
        Task<List<FcrRecord>> GetFcrRecordsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
        Task<DateTime?> GetLastHourUtcAsync(CancellationToken ct = default);
    }
}