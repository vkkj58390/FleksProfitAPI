namespace FleksProfitAPI.Models
{
    public class ProfitResult
    {
        public double AverageFcrPriceDKKPerMWHour { get; set; }
        public double FcrRevenueDKK { get; set; }

        public double AverageBuyPriceDKKPerKWh { get; set; }
        public double AverageSellSpotPriceDKKPerKWh { get; set; }

        public double ArbitrageProfitGrossDKK { get; set; }
        public double AggregatorFeeDKK { get; set; }
        public double TotalNetProfitDKK { get; set; }
    }
}