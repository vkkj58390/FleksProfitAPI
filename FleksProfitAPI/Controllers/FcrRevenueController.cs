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
            if (request.CapacityKW <= 0) return BadRequest("Capacity must be > 0.");
            if (request.DaysPerMonth <= 0) return BadRequest("DaysPerMonth must be > 0.");

            // Timeinterval kræves (tillad 0/0 = hele døgnet)
            if (!request.HourStart.HasValue || !request.HourEnd.HasValue)
                return BadRequest("HourStart and HourEnd are required. Use 0/0 for full day.");

            var start = request.HourStart.Value;
            var end = request.HourEnd.Value;

            // Valider grænser: start 0..23, end 0..24 (end==0 kun hvis 0/0)
            if (start < 0 || start > 23) return BadRequest("HourStart must be in [0,23].");
            if (end < 0 || end > 24) return BadRequest("HourEnd must be in [0,24].");

            // 0/0 = hele døgnet
            var isFullDay = (start == 0 && end == 0);
            if (!isFullDay)
            {
                // start == end => 0 timer (ikke gyldigt)
                if (start == end)
                    return BadRequest("HourStart and HourEnd cannot be equal unless both are 0 (0/0 = full day).");

                // end==0 uden 0/0 er ikke meningsfuldt
                if (end == 0)
                    return BadRequest("HourEnd=0 is only allowed with 0/0 (full day). Use 24 to represent end of day.");
            }

            var result = await _revenueService.CalculateRevenueAsync(request);
            return Ok(result);
        }
    }
}
