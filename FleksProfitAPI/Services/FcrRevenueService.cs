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
        /// </summary>
        public async Task<RevenueResult> CalculateRevenueAsync(RevenueRequest request)
        {
            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var query = _db.FcrRecords
                .Where(r => r.HourUTC.Date >= startDate && r.HourUTC.Date <= endDate);

            // Filtrér på specifikke timer, hvis sat
            if (request.HourStart.HasValue && request.HourEnd.HasValue)
            {
                int hStart = request.HourStart.Value;
                int hEnd = request.HourEnd.Value;
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
