using FleksProfitAPI.Data;

namespace FleksProfitAPI.Services
{
    /// <summary>
    /// Baggrundsservice, der synkroniserer data til QuestDB-tabeller.
    /// Understøtter flere datasæt (FCR, elpriser m.fl.).
    /// </summary>
    public class DbSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DbSyncBackgroundService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromHours(1);

        public DbSyncBackgroundService(IServiceProvider services, ILogger<DbSyncBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        // Henter data ved opstart og derefter hver time og lægger det over i QuestDB
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DB Sync service startet.");

            // Initial bootstrap
            try
            {
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IQuestDbRepository>();
                await repo.EnsureFcrRecordsTableExistsAsync(stoppingToken);

                var fcrService = scope.ServiceProvider.GetRequiredService<FcrDataService>();

                var lastHour = await repo.GetLastFcrHourUtcAsync(stoppingToken);
                if (lastHour == null)
                {
                    var start = new DateTime(2020, 1, 1);
                    var end = DateTime.UtcNow;
                    var added = await fcrService.SyncFcrDataAsync(start, end, stoppingToken);
                    _logger.LogInformation("Initial FCR sync done. {Count} rows inserted.", added);
                }
                else
                {
                    _logger.LogInformation("Data exists. Last hour: {LastHour}", lastHour);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial bootstrap failed, will retry next cycle.");
            }

            // Loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var fcrService = scope.ServiceProvider.GetRequiredService<FcrDataService>();
                    // senere kan man tilføje flere:
                    // var afrrService = scope.ServiceProvider.GetRequiredService<AfrrService>();
                    

                    var repo = scope.ServiceProvider.GetRequiredService<IQuestDbRepository>();
                    await repo.EnsureFcrRecordsTableExistsAsync(); // sikrer tabel hver cyklus
                    
                    // === FCR ===
                    await SyncEnerginetDatasetAsync("FCR", repo, fcrService, stoppingToken);


                    // === aFRR (eksempel, hvis man tilføjer senere) ===
                    // await SyncDatasetAsync("aFRR", db, afrrService, stoppingToken);

                    // === Strom Price Data ===
                    var stromService = scope.ServiceProvider.GetRequiredService<StromPriceDataService>();
                    await repo.EnsureElectricityPricesTableExistsAsync(stoppingToken);
                    await SyncElectricityPricesAsync(stromService, "DK1", stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fejl under synkronisering.");
                }

                _logger.LogInformation("Venter {Hours} time(r) før næste synk...", _updateInterval.TotalHours);
                await Task.Delay(_updateInterval, stoppingToken);
            }

            _logger.LogInformation("DB Sync baggrundsservice stoppet.");
        }

        private async Task SyncEnerginetDatasetAsync(string name, IQuestDbRepository repo, FcrDataService service, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starter synkronisering for {Dataset}", name);

            var lastHourUtc = await repo.GetLastFcrHourUtcAsync(stoppingToken);

            DateTime start;
            DateTime end = DateTime.UtcNow;

            if (lastHourUtc == null)
            {
                start = new DateTime(2020, 1, 1);
                _logger.LogInformation("Første synk for {Dataset} - henter alt data siden {Start}", name, start);
            }
            else
            {
                start = lastHourUtc.Value.AddHours(1);
                _logger.LogInformation("Henter nyt {Dataset}-data fra {Start} til {End}", name, start, end);
            }

            var addedCount = await service.SyncFcrDataAsync(start, end, stoppingToken);
            _logger.LogInformation("{Dataset} synk færdig - {Count} nye rækker tilføjet.", name, addedCount);
        }

        private async Task SyncElectricityPricesAsync(StromPriceDataService stromService, string priceArea, CancellationToken ct)
        {
            try
            {
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IQuestDbRepository>();
                var lastHour = await repo.GetLastElectricityPriceHourUtcAsync(priceArea, ct);

                DateTime start = lastHour == null ? new DateTime(2021, 1, 18) : lastHour.Value.AddHours(1);
                DateTime end = DateTime.UtcNow;

                var added = await stromService.SyncElectricityPricesAsync(priceArea, start, end, ct);
                _logger.LogInformation("Electricity price sync ({PriceArea}) added {Count} rows from {Start} to {End}", priceArea, added, start, end);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing electricity prices {PriceArea}", priceArea);
            }
        }
    }
}
