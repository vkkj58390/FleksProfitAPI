using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FleksProfitAPI.Models
{
    public class RevenueRequest
    {
        /// <summary>Kapacitet i kW, fx 30</summary>
        [Range(0.000001, double.MaxValue, ErrorMessage = "CapacityKW must be > 0.")]
        public double CapacityKW { get; set; }

        /// <summary>Dage pr. måned brugeren vil tilbyde kapaciteten</summary>
        [DefaultValue(30)]
        [Range(1, 31, ErrorMessage = "DaysPerMonth must be in [1,31].")]
        public int DaysPerMonth { get; set; } = 30;

        /// <summary>Antal timer pr. dag, der kommitteres</summary>
        [DefaultValue(24)]
        [Required]
        [Range(1, 24, ErrorMessage = "HoursPerDay must be in [1,24].")]
        public int HoursPerDay { get; set; } = 24;

        /// <summary>Starttime (0-23). 0/0 = hele døgnet.</summary>
        [DefaultValue(0)]
        [Range(0, 23, ErrorMessage = "HourStart must be in [0,23].")]
        public int? HourStart { get; set; } = 0;

        /// <summary>Sluttid (0-24). 0 kun sammen med 0/0 (fuldt døgn).</summary>
        [DefaultValue(0)]
        [Range(0, 24, ErrorMessage = "HourEnd must be in [0,24].")]
        public int? HourEnd { get; set; } = 0;
    }
}
