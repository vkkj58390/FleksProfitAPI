namespace FleksProfitAPI.Models
{
    public class RevenueResult
    {
        public double AveragePriceDKKPerMWHour { get; set; } // Gennemsnitlig pris pr. MW pr. time i DKK
        public double MonthlyRevenueDKK { get; set; }        // Beregnet månedlig revenue for den angivne kapacitet og tidsperiode
    }
}
