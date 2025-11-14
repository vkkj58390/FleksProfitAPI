using FleksProfitAPI.Data;

namespace FleksProfitAPI.Services
{
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EnergiNet Sync baggrundsservice startet.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var fcrService = scope.ServiceProvider.GetRequiredService<FcrDataService>();
                    var repo = scope.ServiceProvider.GetRequiredService<QuestDbRepository>();

                    await SyncDatasetAsync("FCR", repo, fcrService, stoppingToken);
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
