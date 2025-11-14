namespace FleksProfitAPI.Models
{
    public class RevenueResult
    {
        /// <summary>Gennemsnitlig pris pr. MW pr. time i DKK (beregnet for de udvalgte timer per døgn; ved 0/0 er det døgn-gennemsnittet over 24 timer)</summary>
        public double AveragePriceDKKPerMWHour { get; set; }

        /// <summary>Beregnede månedlige indtægter for den angivne kapacitet, ud fra HoursPerDay og DaysPerMonth</summary>
        public double MonthlyRevenueDKK { get; set; }
    }
}
