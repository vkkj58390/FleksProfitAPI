namespace FleksProfitAPI.Models
{
    public class ElectricityPriceRecord
    {
        public DateTime HourUTC { get; set; }
        public string PriceArea { get; set; } = "";
        // Købspris (inkl. alt: spot+afgift+tariffer+moms)
        public double? TotalPriceDKKPerKWh { get; set; }
        // Rå spotpris uden afgifter/moms (basis for salg)
        public double? SpotPriceDKKPerKWh { get; set; }
    }
}