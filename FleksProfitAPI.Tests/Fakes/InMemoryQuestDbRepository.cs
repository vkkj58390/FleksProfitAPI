using FleksProfitAPI.Data;
using FleksProfitAPI.Models;

namespace FleksProfitAPI.Tests.Fakes
{
    public class InMemoryQuestDbRepository : IQuestDbRepository
    {
        private readonly List<FcrRecord> _store = new();

        public Task EnsureTableExistsAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<int> InsertFcrRecordsAsync(IEnumerable<FcrRecord> records, CancellationToken ct = default)
        {
            _store.AddRange(records);
            return Task.FromResult(records.Count());
        }

        public Task<List<FcrRecord>> GetFcrRecordsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
        {
            // QuestDB compares plain timestamps; here we filter inclusively in-memory.
            var result = _store
                .Where(r => r.HourUTC >= startUtc && r.HourUTC <= endUtc)
                .OrderBy(r => r.HourUTC)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<DateTime?> GetLastHourUtcAsync(CancellationToken ct = default)
        {
            if (_store.Count == 0) return Task.FromResult<DateTime?>(null);
            return Task.FromResult<DateTime?>(_store.Max(r => r.HourUTC));
        }

        // Test helpers
        public void Seed(IEnumerable<FcrRecord> records) => _store.AddRange(records);
        public IReadOnlyList<FcrRecord> All => _store;
    }
}