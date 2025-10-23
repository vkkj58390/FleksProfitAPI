using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FleksProfitAPI.Services
{
    public class FcrRevenueService
    {
        private readonly AppDbContext _db;

        public FcrRevenueService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Beregn månedlig revenue baseret på sidste hele måned.
        /// Hvis HourStart og HourEnd er angivet, beregnes kun for de timer.
        /// 0/0 tolkes som "ingen timefiltrering".
        /// </summary>
        public async Task<RevenueResult> CalculateRevenueAsync(RevenueRequest request)
        {
            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var query = _db.FcrRecords
                .Where(r => r.HourUTC.Date >= startDate && r.HourUTC.Date <= endDate);

            // Apply hour filtering only when a real range is provided (exclude 0/0)
            var hasHourRange =
                request.HourStart.HasValue && request.HourEnd.HasValue &&
                !(request.HourStart == 0 && request.HourEnd == 0);

            if (hasHourRange)
            {
                var hStart = request.HourStart!.Value;
                var hEnd = request.HourEnd!.Value;
                query = query.Where(r => r.HourDK.Hour >= hStart && r.HourDK.Hour < hEnd);
            }

            var records = await query.ToListAsync();

            if (!records.Any())
                return new RevenueResult { AveragePriceDKKPerMWHour = 0, MonthlyRevenueDKK = 0 };

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
