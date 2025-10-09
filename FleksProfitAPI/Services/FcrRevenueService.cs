using FleksProfitAPI.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FleksProfitAPI.Services
{
    public class FcrRevenueService
    {
        private readonly AppDbContext _db;

        public FcrRevenueService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Beregn total revenue for en given kapacitet (MW) og tidsperiode.
        /// </summary>
        public async Task<double> CalculateRevenueAsync(double capacityMW, DateTime start, DateTime end)
        {
            var records = await _db.FcrRecords
                .Where(r => r.HourUTC >= start && r.HourUTC < end)
                .ToListAsync();

            double totalRevenue = records.Sum(r => r.FCRdk_DKK.GetValueOrDefault() * capacityMW);

            return totalRevenue;
        }

        /// <summary>
        /// Beregn gennemsnitspris for et specifikt timeinterval fx 12-16 hver dag i perioden.
        /// </summary>
        public async Task<double> CalculateRevenueForHoursAsync(double capacityMW, DateTime start, DateTime end, int hourStart, int hourEnd)
        {
            var records = await _db.FcrRecords
                .Where(r => r.HourUTC >= start && r.HourUTC < end)
                .Where(r => r.HourDK.Hour >= hourStart && r.HourDK.Hour < hourEnd)
                .ToListAsync();

            double totalRevenue = records.Sum(r => r.FCRdk_DKK.GetValueOrDefault() * capacityMW);

            return totalRevenue;
        }
    }
}
