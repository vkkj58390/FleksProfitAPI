using System;
using System.ComponentModel.DataAnnotations;

namespace FleksProfitAPI.Models
{
    public class FcrRecord
    {
        [Key]
        public int Id { get; set; }  // Surrogate key
            
        [Required]
        public DateTime HourUTC { get; set; }

        public DateTime HourDK { get; set; }

        public double? FCRdomestic_MW { get; set; }

        public double? FCRabroad_MW { get; set; }

        public double? FCRcross_EUR { get; set; }

        public double? FCRcross_DKK { get; set; }

        public double? FCRdk_EUR { get; set; }

        public double? FCRdk_DKK { get; set; }
    }
}
