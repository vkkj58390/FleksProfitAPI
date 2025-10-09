namespace FleksProfitAPI.Models
{
    public class RevenueResult
    {
        public double AveragePriceDKKPerMWHour { get; set; } // Gennemsnitspris pr. MW/time
        public double MonthlyRevenueDKK { get; set; }        // Estimeret månedlig revenue
    }
}
