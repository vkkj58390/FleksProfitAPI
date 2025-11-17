using FleksProfitAPI.Data;

namespace FleksProfitAPI.Services
{
    /// <summary>
    /// Baggrundsservice, der synkroniserer data fra EnergiNet til lokale tabeller.
    /// Kan nemt udvides til flere systemydelser (FCR, aFRR, mFRR osv.)
    /// </summary>
    public class EnergiNetSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EnergiNetSyncBackgroundService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromHours(1);

        public EnergiNetSyncBackgroundService(IServiceProvider services, ILogger<EnergiNetSyncBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        // Henter data fra EnergiNet ved opstart og derefter hver time og lægger det over i QuestDB
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EnergiNet Sync service startet.");

            // Initial bootstrap
            try
            {
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<QuestDbRepository>();
                await repo.EnsureTableExistsAsync(stoppingToken);
                var fcrService = scope.ServiceProvider.GetRequiredService<FcrDataService>();

                var lastHour = await repo.GetLastHourUtcAsync(stoppingToken);
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
                    
                    var repo = scope.ServiceProvider.GetRequiredService<QuestDbRepository>();
                    await repo.EnsureTableExistsAsync(); // sikrer tabel hver cyklus
                    
                    // === FCR ===
                    await SyncDatasetAsync("FCR", repo, fcrService, stoppingToken);
                    // === aFRR (eksempel, hvis man tilføjer senere) ===
                    // await SyncDatasetAsync("aFRR", db, afrrService, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fejl under synkronisering fra EnergiNet.");
                }

                _logger.LogInformation("Venter {Hours} time(r) før næste synk...", _updateInterval.TotalHours);
                await Task.Delay(_updateInterval, stoppingToken);
            }

            _logger.LogInformation("EnergiNet Sync baggrundsservice stoppet.");
        }

        private async Task SyncDatasetAsync(string name, QuestDbRepository repo, FcrDataService service, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starter synkronisering for {Dataset}", name);

            var lastHourUtc = await repo.GetLastHourUtcAsync(stoppingToken);

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
    }
}
