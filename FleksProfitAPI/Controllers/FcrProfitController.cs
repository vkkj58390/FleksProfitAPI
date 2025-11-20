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
        public async Task<IActionResult> Calculate([FromBody] FcrProfitRequest request)
        {
            if (request.CapacityKW <= 0) return BadRequest("CapacityKW must be > 0.");
            if (request.DaysPerMonth <= 0) return BadRequest("DaysPerMonth must be > 0.");
            if (request.HoursPerDay <= 0 || request.HoursPerDay > 24) return BadRequest("HoursPerDay must be in [1,24].");
            if (!request.HourStart.HasValue || !request.HourEnd.HasValue) return BadRequest("HourStart and HourEnd required.");
            var s = request.HourStart.Value;
            var e = request.HourEnd.Value;
            if (s < 0 || s > 23) return BadRequest("HourStart in [0,23].");
            if (e < 0 || e > 24) return BadRequest("HourEnd in [0,24].");
            var useDailyAveragePrice = (s == 0 && e == 0);
            if (!useDailyAveragePrice)
            {
                if (s == e) return BadRequest("HourStart != HourEnd unless 0/0.");
                if (e == 0) return BadRequest("HourEnd=0 only allowed with 0/0. Use 24 for end of day.");
                int intervalHours = (s < e) ? e - s : (24 - s) + e;
                if (intervalHours != request.HoursPerDay)
                    return BadRequest($"HoursPerDay={request.HoursPerDay} must equal interval size {intervalHours}.");
            }
            if (Math.Abs(request.ActivationBuyFraction + request.ActivationSellFraction - 1.0) > 0.00001)
                return BadRequest("ActivationBuyFraction + ActivationSellFraction must = 1.");

            var result = await _profitService.CalculateProfitAsync(request);
            return Ok(result);
        }
    }
}