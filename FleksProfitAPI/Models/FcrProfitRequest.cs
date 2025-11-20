using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FleksProfitAPI.Models
{
    public class FcrProfitRequest
    {
        [Range(0.000001, double.MaxValue)]
        public double CapacityKW { get; set; }

        [Range(1, 31)]
        public int DaysPerMonth { get; set; }

        [Range(1, 24)]
        public int HoursPerDay { get; set; }

        [Range(0,23)]
        public int? HourStart { get; set; }

        [Range(0,24)]
        public int? HourEnd { get; set; }

        [DefaultValue(0.8)]
        [Range(0,1)]
        public double ActivationRatio { get; set; } = 0.8;

        [DefaultValue(0.5)]
        [Range(0,1)]
        public double ActivationBuyFraction { get; set; } = 0.5;

        [DefaultValue(0.5)]
        [Range(0,1)]
        public double ActivationSellFraction { get; set; } = 0.5;

        [DefaultValue(0.1)]
        [Range(0,1)]
        public double ActivationCapacityFraction { get; set; } = 0.1;

        [DefaultValue(0.18)]
        [Range(0,1)]
        public double LossFraction { get; set; } = 0.18;

        [DefaultValue(0.20)]
        [Range(0,1)]
        public double AggregatorShare { get; set; } = 0.20;

        [DefaultValue("DK1")]
        public string PriceArea { get; set; } = "DK1";
    }
}