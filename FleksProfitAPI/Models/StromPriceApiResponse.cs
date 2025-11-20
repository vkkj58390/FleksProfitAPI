using System.Text.Json.Serialization;

namespace FleksProfitAPI.Models
{
    // Minimal mapping af Strømligning API respons
    public class StromPriceApiResponse
    {
        [JsonPropertyName("priceArea")]
        public string PriceArea { get; set; } = "";

        [JsonPropertyName("prices")]
        public List<StromPriceEntry> Prices { get; set; } = new();
    }

    public class StromPriceEntry
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("price")]
        public StromPriceValue Price { get; set; } = new();

        [JsonPropertyName("details")]
        public StromPriceDetails Details { get; set; } = new();
    }

    public class StromPriceValue
    {
        [JsonPropertyName("value")]
        public double Value { get; set; }  // Spot base (kr/kWh) ekskl. moms?
        [JsonPropertyName("vat")]
        public double Vat { get; set; }
        [JsonPropertyName("total")]
        public double Total { get; set; }  // Total købspris (inkl. alt)
    }

    public class StromPriceDetails
    {
        [JsonPropertyName("electricity")]
        public StromPriceValue Electricity { get; set; } = new();
    }
}