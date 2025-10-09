using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FleksProfitAPI.Services
{
    /// <summary>
    /// Står for at hente, indsætte og levere FCR-data fra EnergiNet API.
    /// Bruges af EnergiNetSyncBackgroundService til automatisk synkronisering
    /// og af FcrController til visning/pagination.
    /// </summary>
    public class FcrService : EnergiNetBaseService
    {
        private readonly AppDbContext _db;

        public FcrService(HttpClient httpClient, AppDbContext db) : base(httpClient)
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

            var existingTimestamps = await _db.FcrRecords
                .Select(r => r.HourUTC)
                .ToListAsync();

            var freshData = newData
                .Where(d => !existingTimestamps.Contains(d.HourUTC))
                .ToList();

            if (freshData.Any())
            {
                _db.FcrRecords.AddRange(freshData);
                await _db.SaveChangesAsync();
            }

            return freshData.Count;
        }

        /// <summary>
        /// Henter FCR-data pagineret og filtreret efter dato (valgfrit).
        /// </summary>
        public async Task<PagedResult<FcrRecord>> GetPagedAsync(
            DateTime? start = null,
            DateTime? end = null,
            int page = 1,
            int pageSize = 500)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 500;

            var query = _db.FcrRecords.AsQueryable();

            // 🔹 Filtrér på datointerval hvis angivet
            if (start.HasValue)
                query = query.Where(r => r.HourUTC >= start.Value);
            if (end.HasValue)
                query = query.Where(r => r.HourUTC < end.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(r => r.HourUTC)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<FcrRecord>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
