using FleksProfitAPI.Models;
using FleksProfitAPI.Services;
using FleksProfitAPI.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FleksProfitAPI.Tests
{
    [TestClass]
    public class RevenueServiceTests
    {
        private static List<FcrRecord> MakeDay(DateTime dayUtc, double baseDkk)
        {
            var list = new List<FcrRecord>(24);
            for (int h = 0; h < 24; h++)
            {
                list.Add(new FcrRecord
                {
                    HourUTC = dayUtc.AddHours(h),
                    HourDK = dayUtc.AddHours(h), // For tests assume DK=UTC
                    FCRdk_DKK = baseDkk + h      // 100..123 or 200..223 etc.
                });
            }
            return list;
        }

        [TestMethod]
        public async Task CalculateRevenue_AllDay_UsesDailyAverages()
        {
            // Arrange
            var repo = new InMemoryQuestDbRepository();

            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var monthStart = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            repo.Seed(MakeDay(monthStart, 100));
            repo.Seed(MakeDay(monthStart.AddDays(1), 200));

            var service = new FcrRevenueService(repo);
            var req = new RevenueRequest
            {
                CapacityKW = 100, // 0.1 MW
                HoursPerDay = 24,
                DaysPerMonth = 2,
                HourStart = 0,    // 0/0 → full day (no filtering)
                HourEnd = 0
            };

            var expectedAvg = 161.5;
            var expectedRevenue = expectedAvg * (0.1) * 24 * 2;

            // Act
            var res = await service.CalculateRevenueAsync(req);

            // Assert
            Assert.AreEqual(Math.Round(expectedAvg, 1), Math.Round(res.AveragePriceDKKPerMWHour, 1), "Average price mismatch");
            Assert.AreEqual(Math.Round(expectedRevenue, 2), Math.Round(res.MonthlyRevenueDKK, 2), "Monthly revenue mismatch");
        }

        [TestMethod]
        public async Task CalculateRevenue_HourRange_WrapAround_22_06()
        {
            // Arrange
            var repo = new InMemoryQuestDbRepository();

            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            var monthStart = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            repo.Seed(MakeDay(monthStart, 100));

            var service = new FcrRevenueService(repo);
            var req = new RevenueRequest
            {
                CapacityKW = 50,
                HoursPerDay = 8,  // committing 8 hours per day
                DaysPerMonth = 1,
                HourStart = 22,   // wrap-around window [22, 06)
                HourEnd = 6
            };

            var expectedAvg = 107.5;
            var expectedRevenue = expectedAvg * 0.05 * 8 * 1;

            // Act
            var res = await service.CalculateRevenueAsync(req);

            // Assert
            Assert.AreEqual(Math.Round(expectedAvg, 1), Math.Round(res.AveragePriceDKKPerMWHour, 1), "Average price mismatch");
            Assert.AreEqual(Math.Round(expectedRevenue, 2), Math.Round(res.MonthlyRevenueDKK, 2), "Monthly revenue mismatch");
        }

        [TestMethod]
        public async Task CalculateRevenue_NoData_ReturnsZeros()
        {
            // Arrange
            var repo = new InMemoryQuestDbRepository(); // no seed
            var service = new FcrRevenueService(repo);
            var req = new RevenueRequest
            {
                CapacityKW = 100,
                HoursPerDay = 24,
                DaysPerMonth = 30,
                HourStart = 0,
                HourEnd = 0
            };

            // Act
            var res = await service.CalculateRevenueAsync(req);

            // Assert
            Assert.AreEqual(0, res.AveragePriceDKKPerMWHour);
            Assert.AreEqual(0, res.MonthlyRevenueDKK);
        }
    }
}