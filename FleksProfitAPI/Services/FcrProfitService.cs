using FleksProfitAPI.Data;
using FleksProfitAPI.Models;

namespace FleksProfitAPI.Services
{
    public class FcrProfitService
    {
        private readonly IQuestDbRepository _repo;

        public FcrProfitService(IQuestDbRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProfitResult> CalculateProfitAsync(ProfitRequest request)
        {
            // Sidste hele måned
            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var monthStart = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Timestamps for fetch
            var tsStart = DateTime.SpecifyKind(monthStart, DateTimeKind.Unspecified);
            var tsEndInclusive = DateTime.SpecifyKind(monthEnd.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

            // Hent FCR & elpriser
            var fcrRecords = await _repo.GetFcrRecordsAsync(tsStart, tsEndInclusive);
            var priceRecords = await _repo.GetElectricityPricesAsync(tsStart, tsEndInclusive, request.PriceArea);

            // If no data -> zero
            if (fcrRecords.Count == 0 || priceRecords.Count == 0)
            {
                return new ProfitResult
                {
                    AverageFcrPriceDKKPerMWHour = 0,
                    FcrRevenueDKK = 0,
                    AverageBuyPriceDKKPerKWh = 0,
                    AverageSellSpotPriceDKKPerKWh = 0,
                    ArbitrageProfitGrossDKK = 0,
                    AggregatorFeeDKK = 0,
                    TotalNetProfitDKK = 0
                };
            }

            var startHour = request.HourStart!.Value;
            var endHour = request.HourEnd!.Value;
            bool useDailyAveragePrice = (startHour == 0 && endHour == 0);

            // Filtrering af timer
            IEnumerable<FcrRecord> fcrFiltered = fcrRecords;
            IEnumerable<ElectricityPriceRecord> priceFiltered = priceRecords;

            if (!useDailyAveragePrice)
            {
                if (startHour < endHour)
                {
                    fcrFiltered = fcrFiltered.Where(r => r.HourDK.Hour >= startHour && r.HourDK.Hour < endHour);
                    priceFiltered = priceFiltered.Where(p => p.HourUTC.Hour >= startHour && p.HourUTC.Hour < endHour);
                }
                else
                {
                    fcrFiltered = fcrFiltered.Where(r => r.HourDK.Hour >= startHour || r.HourDK.Hour < endHour);
                    priceFiltered = priceFiltered.Where(p => p.HourUTC.Hour >= startHour || p.HourUTC.Hour < endHour);
                }
            }

            var fcrDayAverages = fcrFiltered
                .GroupBy(r => r.HourUTC.Date)
                .Select(g => g.Average(r => r.FCRdk_DKK ?? 0))
                .ToList();

            var avgFcrPrice = fcrDayAverages.Count == 0 ? 0 : fcrDayAverages.Average();

            var avgBuyPrice = priceFiltered
                .Select(p => p.TotalPriceDKKPerKWh ?? 0)
                .DefaultIfEmpty(0)
                .Average();

            var avgSellSpotPrice = priceFiltered
                .Select(p => p.SpotPriceDKKPerKWh ?? 0)
                .DefaultIfEmpty(0)
                .Average();

            var capacityMW = request.CapacityKW / 1000.0;
            var fcrRevenue = avgFcrPrice * capacityMW * request.HoursPerDay * request.DaysPerMonth;

            // Arbitrage (per scheduled hour)
            double activationHoursPerScheduledHourBuy = request.ActivationRatio * request.ActivationBuyFraction;
            double activationHoursPerScheduledHourSell = request.ActivationRatio * request.ActivationSellFraction;

            var tradedPowerKW = request.CapacityKW * request.ActivationCapacityFraction;

            // Køb kWh per scheduled hour = tradedPowerKW * activationHoursPerScheduledHourBuy
            // Salg kWh per scheduled hour = tradedPowerKW * (1 - loss) * activationHoursPerScheduledHourSell (fx 64,484)
            var buyCostPerScheduledHour = tradedPowerKW * activationHoursPerScheduledHourBuy * avgBuyPrice;
            var sellRevenuePerScheduledHour = tradedPowerKW * (1 - request.LossFraction) * activationHoursPerScheduledHourSell * avgSellSpotPrice;

            // 64,484 - 42,3 = 22,184 * 1 * 30 = 665,52
            var arbitrageProfitPerScheduledHour = sellRevenuePerScheduledHour - buyCostPerScheduledHour;
            var arbitrageProfitGross = arbitrageProfitPerScheduledHour * request.HoursPerDay * request.DaysPerMonth;

            var grossTotal = fcrRevenue + arbitrageProfitGross;
            var aggregatorFee = fcrRevenue * request.AggregatorShare;
            var netTotal = grossTotal - aggregatorFee;

            return new ProfitResult
            {
                AverageFcrPriceDKKPerMWHour = avgFcrPrice,
                FcrRevenueDKK = fcrRevenue,
                AverageBuyPriceDKKPerKWh = avgBuyPrice,
                AverageSellSpotPriceDKKPerKWh = avgSellSpotPrice,
                ArbitrageProfitGrossDKK = arbitrageProfitGross,
                AggregatorFeeDKK = aggregatorFee,
                TotalNetProfitDKK = netTotal
            };
        }
    }
}