using System;
using System.Diagnostics;
using System.Threading;
using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using Npgsql;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FleksProfitAPI.Tests;

[TestClass]
[TestCategory("Integration")]
public class RepositoryIntegrationTests
{
    private static IContainer? _quest;
    private static string? _connString;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _quest = new ContainerBuilder()
            .WithImage("questdb/questdb:9.1.0")
            .WithPortBinding(8812, true)
            .WithEnvironment("QDB_PG_USER", "admin")
            .WithEnvironment("QDB_PG_PASSWORD", "admin123")
            .WithEnvironment("QDB_PG_DATABASE", "qdb")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8812))
            .Build();

        _quest.StartAsync().GetAwaiter().GetResult();

        var hostPort = _quest.GetMappedPublicPort(8812);
        Console.WriteLine($"Started QuestDB container id={_quest.Id} mappedPort={hostPort}");

        // Disable pooling for tests to avoid stale connections during container startup/restart
        _connString =
            $"Host=localhost;Port={hostPort};Username=admin;Password=admin123;Database=qdb;Pooling=false;Server Compatibility Mode=NoTypeLoading";

        // Poll until the Postgres/pgwire endpoint accepts connections (fresh non-pooled connection)
        var ds = new NpgsqlDataSourceBuilder(_connString).Build();
        var sw = Stopwatch.StartNew();
        var ready = false;
        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            try
            {
                using var conn = ds.OpenConnection();
                Console.WriteLine("QuestDB: SQL port reachable and accepting connections.");
                ready = true;
                break;
            }
            catch (Exception)
            {
                Thread.Sleep(500);
            }
        }

        if (!ready)
            throw new InvalidOperationException("QuestDB did not become ready within the timeout.");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _quest?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [TestMethod]
    public void Ensure_Insert_Read_Works()
    {
        // Arrange
        Assert.IsNotNull(_connString, "Connection string not initialized");
        var ds = new NpgsqlDataSourceBuilder(_connString!).Build();
        var repo = new QuestDbRepository(ds);
        repo.EnsureTableExistsAsync().GetAwaiter().GetResult();

        var t0 = new DateTime(2025, 1, 1, 0, 0, 0);
        var input = new List<FcrRecord>
        {
            new FcrRecord { HourUTC = t0,             HourDK = t0,             FCRdk_DKK = 100 },
            new FcrRecord { HourUTC = t0.AddHours(1), HourDK = t0.AddHours(1), FCRdk_DKK = 110 },
        };

        // Act
        var inserted = repo.InsertFcrRecordsAsync(input).GetAwaiter().GetResult();


        #region
        // quick sync check with short retry
        List<(DateTime ts, double? price)> stored = new();
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            using var connCheck = ds.OpenConnection();
            using var cmdCount = new NpgsqlCommand("SELECT count(*) FROM fcrrecords;", connCheck);
            var cnt = Convert.ToInt32(cmdCount.ExecuteScalar() ?? 0);
            if (cnt > 0)
            {
                using var cmd2 = new NpgsqlCommand("SELECT hourutc, fcrdk_dkk FROM fcrrecords ORDER BY hourutc;", connCheck);
                using var r = cmd2.ExecuteReader();
                while (r.Read()) stored.Add((r.GetDateTime(0), r.IsDBNull(1) ? null : (double?)r.GetDouble(1)));
                break;
            }
            Thread.Sleep(200);
        }
        Console.WriteLine($"ROW COUNT AFTER INSERT (observed): {stored.Count}");
        #endregion

        var read = repo.GetFcrRecordsAsync(t0, t0.AddHours(2)).GetAwaiter().GetResult();

        // Assert
        Assert.AreEqual(2, inserted);
        Assert.AreEqual(2, read.Count);
        Assert.AreEqual(100, read[0].FCRdk_DKK);
        Assert.AreEqual(110, read[1].FCRdk_DKK);
    }
}