using FleksProfitAPI.Data;
using Microsoft.EntityFrameworkCore;

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
        private readonly TimeSpan _updateInterval = TimeSpan.FromHours(1); // fx 1 gang i timen

        public EnergiNetSyncBackgroundService(IServiceProvider services, ILogger<EnergiNetSyncBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EnergiNet Sync baggrundsservice startet.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var fcrService = scope.ServiceProvider.GetRequiredService<FcrDataService>();
                    // senere kan man tilføje flere:
                    // var afrrService = scope.ServiceProvider.GetRequiredService<AfrrService>();

                    // === FCR ===
                    await SyncDatasetAsync("FCR", db, fcrService, stoppingToken);

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

        private async Task SyncDatasetAsync(string name, AppDbContext db, FcrDataService service, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starter synkronisering for {Dataset}", name);

            var lastRecord = await db.FcrRecords
                .OrderByDescending(r => r.HourUTC)
                .FirstOrDefaultAsync(stoppingToken);

            DateTime start;
            DateTime end = DateTime.UtcNow;

            if (lastRecord == null)
            {
                start = new DateTime(2020, 1, 1); // hent alt første gang
                _logger.LogInformation("Første synk for {Dataset} - henter alt data siden {Start}", name, start);
            }
            else
            {
                start = lastRecord.HourUTC.AddHours(1);
                _logger.LogInformation("Henter nyt {Dataset}-data fra {Start} til {End}", name, start, end);
            }

            var addedCount = await service.SyncFcrDataAsync(start, end);
            _logger.LogInformation("{Dataset} synk færdig - {Count} nye rækker tilføjet.", name, addedCount);
        }
    }
}
