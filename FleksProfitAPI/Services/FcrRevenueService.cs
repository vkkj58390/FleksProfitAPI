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
        /// Understøtter wrap-around intervaller (fx 22-06) og 0/0 = hele døgnet.
        /// </summary>
        public async Task<RevenueResult> CalculateRevenueAsync(RevenueRequest request)
        {
            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var query = _db.FcrRecords
                .Where(r => r.HourUTC.Date >= startDate && r.HourUTC.Date <= endDate);

            var start = request.HourStart!.Value;
            var end = request.HourEnd!.Value;

            bool fullDay = (start == 0 && end == 0);
            int hoursPerDay;

            if (fullDay)
            {
                // Ingen timefiltrering
                hoursPerDay = 24;
            }
            else if (start < end)
            {
                // Ikke-wrap interval, fx 0-6
                query = query.Where(r => r.HourDK.Hour >= start && r.HourDK.Hour < end);
                hoursPerDay = end - start;
            }
            else
            {
                // Wrap-around interval, fx 22-06
                query = query.Where(r => r.HourDK.Hour >= start || r.HourDK.Hour < end);
                hoursPerDay = (24 - start) + end;
            }

            var records = await query.ToListAsync();

            if (!records.Any())
            {
                return new RevenueResult
                {
                    AveragePriceDKKPerMWHour = 0,
                    MonthlyRevenueDKK = 0,
                    HoursPerDayCalculated = hoursPerDay
                };
            }

            // Gennemsnit pr. dag af FCRdk_DKK
            var dailyAverages = records
                .GroupBy(r => r.HourUTC.Date)
                .Select(g => g.Average(r => r.FCRdk_DKK ?? 0))
                .ToList();

            var averagePricePerMWPerHour = dailyAverages.Average();

            var capacityMW = request.CapacityKW / 1000.0;
            var monthlyRevenue = averagePricePerMWPerHour * capacityMW * hoursPerDay * request.DaysPerMonth;

            return new RevenueResult
            {
                AveragePriceDKKPerMWHour = averagePricePerMWPerHour,
                MonthlyRevenueDKK = monthlyRevenue,
                HoursPerDayCalculated = hoursPerDay
            };
        }
    }
}
