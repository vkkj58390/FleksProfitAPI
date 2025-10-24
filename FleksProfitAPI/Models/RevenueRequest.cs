namespace FleksProfitAPI.Models
{
    public class RevenueRequest
    {
        public double CapacityKW { get; set; } // Kapacitet i kW, fx 30
        public int DaysPerMonth { get; set; }  // Dage pr. måned brugeren vil tilbyde kapaciteten

        // Timeinterval (påkrævet i controlleren, 0/0 = hele døgnet)
        public int? HourStart { get; set; }    // 0-23
        public int? HourEnd { get; set; }      // 0-24 (0 kun sammen med 0/0)
    }
}
