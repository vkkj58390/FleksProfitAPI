using FleksProfitAPI.Data;
using FleksProfitAPI.Models;

namespace FleksProfitAPI.Tests.Fakes
{
    public class InMemoryQuestDbRepository : IQuestDbRepository
    {
        private readonly List<FcrRecord> _store = new();

        public Task EnsureFcrRecordsTableExistsAsync(CancellationToken ct = default) => Task.CompletedTask;

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

        public Task<DateTime?> GetLastFcrHourUtcAsync(CancellationToken ct = default)
        {
            if (_store.Count == 0) return Task.FromResult<DateTime?>(null);
            return Task.FromResult<DateTime?>(_store.Max(r => r.HourUTC));
        }

        // Test helpers
        public void Seed(IEnumerable<FcrRecord> records) => _store.AddRange(records);


        // Nedenstående er ikke-implementerede metoder for elpriser
        public Task EnsureElectricityPricesTableExistsAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<DateTime?> GetLastElectricityPriceHourUtcAsync(string priceArea, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> InsertElectricityPricesAsync(IEnumerable<ElectricityPriceRecord> records, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<ElectricityPriceRecord>> GetElectricityPricesAsync(DateTime startUtc, DateTime endUtc, string priceArea, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<FcrRecord> All => _store;
    }
}