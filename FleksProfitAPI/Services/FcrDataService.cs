using FleksProfitAPI.Data;
using FleksProfitAPI.Models;

namespace FleksProfitAPI.Services
{
    public class FcrDataService : EnergiNetBaseService
    {
        private readonly QuestDbRepository _repo;

        public FcrDataService(HttpClient httpClient, QuestDbRepository repo) : base(httpClient)
        {
            _repo = repo;
        }

        public async Task<int> SyncFcrDataAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            var newData = await FetchDataAsync<FcrRecord>("FcrDK1", start, end, cancellationToken);
            if (newData == null || !newData.Any())
                return 0;

            // Repository expects 'timestamp' params (Unspecified kind)
            var existing = await _repo.GetFcrRecordsAsync(
                DateTime.SpecifyKind(start, DateTimeKind.Unspecified),
                DateTime.SpecifyKind(end,   DateTimeKind.Unspecified),
                cancellationToken);

            var existingTicks = existing.Select(r => r.HourUTC.Ticks).ToHashSet();

            var freshData = newData
                .Where(d => !existingTicks.Contains(d.HourUTC.Ticks))
                .ToList();

            if (freshData.Count == 0)
                return 0;

            return await _repo.InsertFcrRecordsAsync(freshData, cancellationToken);
        }
    }
}
