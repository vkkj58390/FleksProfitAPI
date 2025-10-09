using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FleksProfitAPI.Services
{
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

        /// <summary>
        /// Beregner månedlig estimeret revenue baseret på sidste hele måned.
        /// </summary>
        public async Task<RevenueResult> CalculateMonthlyRevenueAsync(RevenueRequest request)
        {
            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var records = await _db.FcrRecords
                .Where(r => r.HourUTC.Date >= startDate && r.HourUTC.Date <= endDate)
                .ToListAsync();

            if (!records.Any())
                return new RevenueResult
                {
                    AveragePriceDKKPerMWHour = 0,
                    MonthlyRevenueDKK = 0
                };

            // Gennemsnit pr. dag
            var dailyAverages = records
                .GroupBy(r => r.HourUTC.Date)
                .Select(g => g.Average(r => r.FCRdk_DKK ?? 0))
                .ToList();

            var averagePricePerMWPerHour = dailyAverages.Average();

            var capacityMW = request.CapacityKW / 1000.0;
            var monthlyRevenue = averagePricePerMWPerHour * capacityMW * request.HoursPerDay * request.DaysPerMonth;

            return new RevenueResult
            {
                AveragePriceDKKPerMWHour = averagePricePerMWPerHour,
                MonthlyRevenueDKK = monthlyRevenue
            };
        }
    }
}
