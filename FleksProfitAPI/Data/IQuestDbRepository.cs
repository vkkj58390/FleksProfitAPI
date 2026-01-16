using FleksProfitAPI.Models;

namespace FleksProfitAPI.Data
{
    public interface IQuestDbRepository
    {
        Task EnsureFcrRecordsTableExistsAsync(CancellationToken ct = default);
        Task<DateTime?> GetLastFcrHourUtcAsync(CancellationToken ct = default);
        Task<int> InsertFcrRecordsAsync(IEnumerable<FcrRecord> records, CancellationToken ct = default);
        Task<List<FcrRecord>> GetFcrRecordsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
        
        Task EnsureElectricityPricesTableExistsAsync (CancellationToken ct = default);
        Task<DateTime?> GetLastElectricityPriceHourUtcAsync(string priceArea, CancellationToken ct = default);
        Task<int> InsertElectricityPricesAsync(IEnumerable<ElectricityPriceRecord> records, CancellationToken ct = default);
        Task<List<ElectricityPriceRecord>> GetElectricityPricesAsync(DateTime startUtc, DateTime endUtc, string priceArea, CancellationToken ct = default);
    }
}