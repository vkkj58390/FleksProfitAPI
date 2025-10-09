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
        public async Task<int> SyncFcrDataAsync(DateTime start, DateTime end)
        {
            var newData = await FetchDataAsync<FcrRecord>("FcrDK1", start, end);
            if (newData == null || !newData.Any())
                return 0;

            var existingHours = await _db.FcrRecords
                .Select(r => r.HourUTC)
                .ToListAsync();

            var freshData = newData
                .Where(d => !existingHours.Contains(d.HourUTC))
                .ToList();

            if (freshData.Any())
            {
                _db.FcrRecords.AddRange(freshData);
                await _db.SaveChangesAsync();
            }

            return freshData.Count;
        }
    }
}
