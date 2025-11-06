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
        /// Ved 0/0 beregnes timeprisen som gennemsnit over alle 24 timer.
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

            if (!fullDay)
            {
                if (start < end)
                {
                    // Ikke-wrap interval, fx 0-6
                    query = query.Where(r => r.HourDK.Hour >= start && r.HourDK.Hour < end);
                }
                else
                {
                    // Wrap-around interval, fx 22-06
                    query = query.Where(r => r.HourDK.Hour >= start || r.HourDK.Hour < end);
                }
            }
            // Ved fullDay foretages ingen timefiltrering => gennemsnit over alle 24 timer

            var records = await query.ToListAsync();

            if (!records.Any())
            {
                return new RevenueResult
                {
                    AveragePriceDKKPerMWHour = 0,
                    MonthlyRevenueDKK = 0
                };
            }

            // Gennemsnit pr. dag af FCRdk_DKK for de valgte timer (eller hele døgnet ved 0/0)
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
