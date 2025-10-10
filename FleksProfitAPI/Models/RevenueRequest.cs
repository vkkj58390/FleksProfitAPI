namespace FleksProfitAPI.Models
{
    public class RevenueRequest
    {
        public double CapacityKW { get; set; } // Kapacitet i kW, fx 30
        public int HoursPerDay { get; set; }   // Timer pr. dag brugeren vil tilbyde kapaciteten
        public int DaysPerMonth { get; set; }  // Dage pr. måned brugeren vil tilbyde kapaciteten
        public int? HourStart { get; set; }    // Starttime for specifikke timer (0-23)
        public int? HourEnd { get; set; }      // Sluttid for specifikke timer (1-24)
    }
}
