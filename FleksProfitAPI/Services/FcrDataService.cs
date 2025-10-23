using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FleksProfitAPI.Services
{
    public class FcrDataService : EnergiNetBaseService
    {
        private readonly AppDbContext _db;

        public FcrDataService(HttpClient httpClient, AppDbContext db) : base(httpClient)
        {
            _db = db;
        }

        /// <summary>
        /// Synkroniserer FCR-data fra EnergiNet for perioden [start, end].
        /// Returnerer antal nye rækker indsat i databasen.
        /// </summary>
        public async Task<int> SyncFcrDataAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            var newData = await FetchDataAsync<FcrRecord>("FcrDK1", start, end, cancellationToken);
            if (newData == null || !newData.Any())
                return 0;

            // Only load existing hours in the requested window to avoid scanning the whole table
            var existingHours = await _db.FcrRecords
                .AsNoTracking()
                .Where(r => r.HourUTC >= start && r.HourUTC <= end)
                .Select(r => r.HourUTC)
                .ToListAsync(cancellationToken);

            var freshData = newData
                .Where(d => !existingHours.Contains(d.HourUTC))
                .ToList();

            if (freshData.Any())
            {
                _db.FcrRecords.AddRange(freshData);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return freshData.Count;
        }
    }
}
