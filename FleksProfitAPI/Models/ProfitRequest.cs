using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FleksProfitAPI.Models
{
    public class ProfitRequest
    {
        /// <summary>Kapacitet i kW, fx 30</summary>
        [Range(0.000001, double.MaxValue, ErrorMessage = "CapacityKW must be > 0.")]
        public double CapacityKW { get; set; }


        /// <summary>Dage pr. måned brugeren vil tilbyde kapaciteten (skal være > 0)</summary>
        [DefaultValue(0)] // Vises som 0 i Swagger
        [Range(1, 31, ErrorMessage = "DaysPerMonth must be in [1,31].")]
        public int DaysPerMonth { get; set; } = 0; // Initial 0 → kræver input


        /// <summary>Antal timer pr. dag der kommitteres (skal være > 0)</summary>
        [DefaultValue(0)] // Vises som 0 i Swagger
        [Range(1, 24, ErrorMessage = "HoursPerDay must be in [1,24].")]
        public int HoursPerDay { get; set; } = 0; // Initial 0 → kræver input


        /// <summary>Starttime (0-23). 0/0 = hele døgnet.</summary>
        [DefaultValue(0)]
        [Range(0, 23, ErrorMessage = "HourStart must be in [0,23].")]
        public int? HourStart { get; set; } = 0;


        /// <summary>Sluttid (0-24). 0 kun sammen med 0/0 (fuldt døgn).</summary>
        [DefaultValue(0)]
        [Range(0, 24, ErrorMessage = "HourEnd must be in [0,24].")]
        public int? HourEnd { get; set; } = 0;


        /// <summary>Forventet aktiveringsgrad (andel af tid)</summary>
        [DefaultValue(0.8)]
        [Range(0, 1, ErrorMessage = "ActivationRatio must be in [0,1].")]
        public double ActivationRatio { get; set; } = 0.8;


        /// <summary>Andel af aktivering som er køb (import)</summary>
        [DefaultValue(0.5)]
        [Range(0, 1, ErrorMessage = "ActivationBuyFraction must be in [0,1].")]
        public double ActivationBuyFraction { get; set; } = 0.5;


        /// <summary>Andel af aktivering som er salg (export)</summary>
        [DefaultValue(0.5)]
        [Range(0, 1, ErrorMessage = "ActivationSellFraction must be in [0,1].")]
        public double ActivationSellFraction { get; set; } = 0.5;


        /// <summary>Andel af kapaciteten der aktiveres ved aktivering</summary>
        [DefaultValue(0.1)]
        [Range(0, 1, ErrorMessage = "ActivationCapacityFraction must be in [0,1].")]
        public double ActivationCapacityFraction { get; set; } = 0.1;


        /// <summary>Tab ved batteribrug (energitab)</summary>
        [DefaultValue(0.18)]
        [Range(0, 1, ErrorMessage = "LossFraction must be in [0,1].")]
        public double LossFraction { get; set; } = 0.18;


        /// <summary>Aggregatorens cut af indtægten</summary>
        [DefaultValue(0.20)]
        [Range(0, 1, ErrorMessage = "AggregatorShare must be in [0,1].")]
        public double AggregatorShare { get; set; } = 0.20;


        /// <summary>Prisområde (fx DK1 eller DK2)</summary>
        [DefaultValue("DK1")]
        public string PriceArea { get; set; } = "DK1";
    }
}