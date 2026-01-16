using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using FleksProfitAPI.Services;
using FleksProfitAPI.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.RegularExpressions;

namespace FleksProfitAPI.Tests
{
    // Test subclass that fakes EnergiNet fetch (no HTTP).
    public class FakeFcrDataService : FcrDataService
    {
        private readonly List<FcrRecord> _toReturn;

        public FakeFcrDataService(IQuestDbRepository repo, IEnumerable<FcrRecord> toReturn)
            : base(new HttpClient(), repo)
        {
            _toReturn = toReturn.ToList();
        }

        // Replace remote fetch with fixed in-memory data
        protected override Task<List<T>> FetchDataAsync<T>(string datasetName, DateTime start, DateTime end, CancellationToken cancellationToken = default)
            => Task.FromResult(_toReturn.Cast<T>().ToList());
    }

    [TestClass]
    public class FcrDataServiceTests
    {
        [TestMethod]
        public async Task Sync_InsertsOnlyFreshHours()
        {
            // Arrange
            var repo = new InMemoryQuestDbRepository();
            var baseDay = new DateTime(2025, 1, 1);

            // Existing row at hour 0
            repo.Seed(new[]
            {
                new FcrRecord { HourUTC = baseDay.AddHours(0), HourDK = baseDay.AddHours(0), FCRdk_DKK = 100 }
            });

            // Incoming rows hours 0,1,2 (0 is duplicate)
            var incoming = new List<FcrRecord>
            {
                new FcrRecord { HourUTC = baseDay.AddHours(0), HourDK = baseDay.AddHours(0), FCRdk_DKK = 100 },
                new FcrRecord { HourUTC = baseDay.AddHours(1), HourDK = baseDay.AddHours(1), FCRdk_DKK = 110 },
                new FcrRecord { HourUTC = baseDay.AddHours(2), HourDK = baseDay.AddHours(2), FCRdk_DKK = 120 },
            };

            var svc = new FakeFcrDataService(repo, incoming);

            // Act
            var inserted = await svc.SyncFcrDataAsync(baseDay, baseDay.AddHours(3));

            // Assert
            Assert.AreEqual(2, inserted, "Should insert (only new rows) --> 1 and 2");
            Assert.AreEqual(3, repo.All.Count, "Total rows should be 3 after insert");
        }

        [TestMethod]
        public async Task Sync_NoFetchedData_YieldsZero()
        {
            // Arrange
            var repo = new InMemoryQuestDbRepository();
            var svc = new FakeFcrDataService(repo, Enumerable.Empty<FcrRecord>());

            // Act
            var inserted = await svc.SyncFcrDataAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

            // Assert
            Assert.AreEqual(0, inserted);
        }
    }
}