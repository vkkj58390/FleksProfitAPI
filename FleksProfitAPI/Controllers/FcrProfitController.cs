using FleksProfitAPI.Models;
using FleksProfitAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FleksProfitAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FcrProfitController : ControllerBase
    {
        private readonly FcrProfitService _profitService;

        public FcrProfitController(FcrProfitService profitService)
        {
            _profitService = profitService;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] ProfitRequest request)
        {
            if (request.CapacityKW <= 0) return BadRequest("CapacityKW must be > 0.");
            if (request.DaysPerMonth <= 0) return BadRequest("DaysPerMonth must be > 0.");
            if (request.HoursPerDay <= 0 || request.HoursPerDay > 24)
                return BadRequest("HoursPerDay must be in [1,24].");

            // Timeinterval (tillad 0/0 = gennemsnits timepris over hele døgnet)
            if (!request.HourStart.HasValue || !request.HourEnd.HasValue)
                return BadRequest("HourStart and HourEnd are required. Use 0/0 for a 24-hour full-day average.");

            var s = request.HourStart.Value;
            var e = request.HourEnd.Value;

            if (s < 0 || s > 23) return BadRequest("HourStart must be in [0,23].");
            if (e < 0 || e > 24) return BadRequest("HourEnd must be in [0,24].");

            var useDailyAveragePrice = (s == 0 && e == 0);
            if (!useDailyAveragePrice)
            {
                // start == end => 0 timer (ikke gyldigt)
                if (s == e)
                    return BadRequest("HourStart and HourEnd cannot be equal unless both are 0 (0/0 = 24-hour average price).");

                // end==0 uden 0/0 (ikke gyldigt)
                if (e == 0)
                    return BadRequest("HourEnd=0 is only allowed with 0/0 (24-hour average price). Use 24 to represent end of day.");

                // Intervalstørrelse skal matche HoursPerDay
                int intervalHours = (s < e) ? (e - s) : (24 - s) + e;

                if (intervalHours != request.HoursPerDay)
                    return BadRequest($"HoursPerDay={request.HoursPerDay} must equal the selected interval ({intervalHours} hours) derived from HourStart={s} and HourEnd={e}. For wrap-around intervals it is computed across midnight.");
            }

            // Aktiveringsfraktioner skal summe til 1
            if (Math.Abs(request.ActivationBuyFraction + request.ActivationSellFraction - 1.0) > 0.00001)
                return BadRequest("ActivationBuyFraction + ActivationSellFraction must equal 1.");

            var result = await _profitService.CalculateProfitAsync(request);
            return Ok(result);
        }
    }
}