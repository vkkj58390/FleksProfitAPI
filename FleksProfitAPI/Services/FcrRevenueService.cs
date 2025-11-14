using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        /// Hvis HourStart og HourEnd er angivet, beregnes kun for de timer.
        /// 0/0 tolkes som "ingen timefiltrering".
        /// </summary>
        public async Task<RevenueResult> CalculateRevenueAsync(RevenueRequest request)
        {
            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Use Unspecified for 'timestamp' params
            var startTs = DateTime.SpecifyKind(startDate, DateTimeKind.Unspecified);
            var endTs   = DateTime.SpecifyKind(endDate.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

            var records = await _repo.GetFcrRecordsAsync(startTs, endTs);

            var hasHourRange =
                request.HourStart.HasValue && request.HourEnd.HasValue &&
                !(request.HourStart == 0 && request.HourEnd == 0);

            if (hasHourRange)
            {
                var hStart = request.HourStart!.Value;
                var hEnd = request.HourEnd!.Value;
                records = records
                    .Where(r => r.HourDK.Hour >= hStart && r.HourDK.Hour < hEnd)
                    .ToList();
            }

            if (!records.Any())
                return new RevenueResult { AveragePriceDKKPerMWHour = 0, MonthlyRevenueDKK = 0 };

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
