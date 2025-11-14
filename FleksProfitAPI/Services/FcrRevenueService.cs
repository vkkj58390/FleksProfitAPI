using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FleksProfitAPI.Services
{
    public class FcrRevenueService
    {
        private readonly QuestDbRepository _repo;

        public FcrRevenueService(QuestDbRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Beregn månedlig revenue baseret på sidste hele måned.
        /// Understøtter wrap-around intervaller (fx 22-06) og 0/0 = hele døgnet.
        /// Ved 0/0 beregnes timeprisen som gennemsnit over alle 24 timer, men HoursPerDay bruges i indtægtsberegningen.
        /// </summary>
        public async Task<RevenueResult> CalculateRevenueAsync(RevenueRequest request)
        {
            // Afdæk sidste hele kalendermåned
            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var monthStart = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // QuestDB forventer 'timestamp' med Kind=Unspecified
            var tsStart = DateTime.SpecifyKind(monthStart, DateTimeKind.Unspecified);
            var tsEndInclusive = DateTime.SpecifyKind(monthEnd.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

            var records = await _repo.GetFcrRecordsAsync(tsStart, tsEndInclusive);
            if (records == null || records.Count == 0)
            {
                return new RevenueResult
                {
                    AveragePriceDKKPerMWHour = 0,
                    MonthlyRevenueDKK = 0
                };
            }

            // Timefiltrering
            var start = request.HourStart!.Value;
            var end = request.HourEnd!.Value;
            bool fullDay = (start == 0 && end == 0);

            List<FcrRecord> filtered = records;
            if (!fullDay)
            {
                if (start < end)
                {
                    // Ikke-wrap: [start, end)
                    filtered = records
                        .Where(r => r.HourDK.Hour >= start && r.HourDK.Hour < end)
                        .ToList();
                }
                else
                {
                    // Wrap-around: [start, 24) U [0, end)
                    filtered = records
                        .Where(r => r.HourDK.Hour >= start || r.HourDK.Hour < end)
                        .ToList();
                }
            }

            if (filtered.Count == 0)
            {
                return new RevenueResult
                {
                    AveragePriceDKKPerMWHour = 0,
                    MonthlyRevenueDKK = 0
                };
            }

            // Gennemsnit pr. dag af FCRdk_DKK for de valgte timer (eller hele døgnet ved 0/0)
            var dailyAverages = filtered
                .GroupBy(r => r.HourUTC.Date)
                .Select(g => g.Average(r => r.FCRdk_DKK ?? 0))
                .ToList();

            var averagePricePerMWPerHour = dailyAverages.Average();
            var capacityMW = request.CapacityKW / 1000.0;

            var monthlyRevenue = averagePricePerMWPerHour
                                 * capacityMW
                                 * request.HoursPerDay
                                 * request.DaysPerMonth;

            return new RevenueResult
            {
                AveragePriceDKKPerMWHour = averagePricePerMWPerHour,
                MonthlyRevenueDKK = monthlyRevenue
            };
        }
    }
}
