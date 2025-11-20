using FleksProfitAPI.Models;
using FleksProfitAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FleksProfitAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FcrRevenueController : ControllerBase
    {
        private readonly FcrRevenueService _revenueService;

        public FcrRevenueController(FcrRevenueService revenueService)
        {
            _revenueService = revenueService;
        }

        /// <summary>
        /// Beregn månedlig estimeret revenue for sidste hele måned for et givent timeinterval.
        /// Understøtter både ikke-wrap (fx 0-6) og wrap (fx 22-06). 0/0 = hele døgnet.
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateRevenue([FromBody] RevenueRequest request)
        {
            if (request.CapacityKW <= 0) return BadRequest("CapacityKW must be > 0.");
            if (request.DaysPerMonth <= 0) return BadRequest("DaysPerMonth must be > 0.");
            if (request.HoursPerDay <= 0 || request.HoursPerDay > 24)
                return BadRequest("HoursPerDay must be in [1,24].");

            // Timeinterval (tillad 0/0 = gennemsnits timepris over hele døgnet)
            if (!request.HourStart.HasValue || !request.HourEnd.HasValue)
                return BadRequest("HourStart and HourEnd are required. Use 0/0 for a 24-hour full-day average.");

            var start = request.HourStart.Value;
            var end = request.HourEnd.Value;

            if (start < 0 || start > 23) return BadRequest("HourStart must be in [0,23].");
            if (end < 0 || end > 24) return BadRequest("HourEnd must be in [0,24].");

            var useDailyAveragePrice = (start == 0 && end == 0);
            if (!useDailyAveragePrice)
            {
                // start == end => 0 timer (ikke gyldigt)
                if (start == end)
                    return BadRequest("HourStart and HourEnd cannot be equal unless both are 0 (0/0 = 24-hour average price).");

                // end==0 uden 0/0 (ikke gyldigt)
                if (end == 0)
                    return BadRequest("HourEnd=0 is only allowed with 0/0 (24-hour average price). Use 24 to represent end of day.");

                // Intervalstørrelse skal matche HoursPerDay
                int intervalHours = (start < end)
                    ? (end - start)
                    : (24 - start) + end;

                if (intervalHours != request.HoursPerDay)
                    return BadRequest($"HoursPerDay={request.HoursPerDay} must equal the selected interval ({intervalHours} hours) derived from HourStart={start} and HourEnd={end}. For wrap-around intervals it is computed across midnight.");
            }
            
            // Ved 0/0 bruges døgnets 24-timers gennemsnit som timepris, mens HoursPerDay bestemmer hvor mange timer pr. dag der udbetales.
            var result = await _revenueService.CalculateRevenueAsync(request);
            return Ok(result);
        }
    }
}
