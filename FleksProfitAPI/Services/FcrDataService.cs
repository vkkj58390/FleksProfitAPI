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
            Console.WriteLine($"Starting SyncFcrDataAsync from {start:O} to {end:O}");

            var newData = await FetchDataAsync<FcrRecord>("FcrDK1", start, end, cancellationToken);

            if (newData == null || !newData.Any())
            {
                Console.WriteLine("No data fetched from Energinet API");
                return 0;
            }

            Console.WriteLine($"Fetched {newData.Count} records from Energinet API");

            var existing = await _repo.GetFcrRecordsAsync(
                DateTime.SpecifyKind(start, DateTimeKind.Unspecified),
                DateTime.SpecifyKind(end, DateTimeKind.Unspecified),
                cancellationToken);

            Console.WriteLine($"Found {existing.Count} existing records in QuestDB");

            var existingTicks = existing.Select(r => r.HourUTC.Ticks).ToHashSet();

            var freshData = newData
                .Where(d => !existingTicks.Contains(d.HourUTC.Ticks))
                .ToList();

            Console.WriteLine($"Inserting {freshData.Count} new records into QuestDB");

            if (freshData.Count == 0)
                return 0;

            return await _repo.InsertFcrRecordsAsync(freshData, cancellationToken);
        }
    }
}
