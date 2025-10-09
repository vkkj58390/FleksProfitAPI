namespace FleksProfitAPI.Models
{
    public class RevenueRequest
    {
        public double CapacityKW { get; set; }       // Hvor meget kapacitet brugeren vil tilbyde
        public int HoursPerDay { get; set; }        // Hvor mange timer per dag
        public int DaysPerMonth { get; set; }       // Hvor mange dage i måneden brugeren vil tilbyde kapacitet
    }
}
