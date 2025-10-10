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
        /// Beregn månedlig estimeret revenue for hele døgnet eller specifikke timer.
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateRevenue([FromBody] RevenueRequest request)
        {
            if (request.CapacityKW <= 0) return BadRequest("Capacity must be > 0.");
            if (request.HoursPerDay <= 0) return BadRequest("HoursPerDay must be > 0.");
            if (request.DaysPerMonth <= 0) return BadRequest("DaysPerMonth must be > 0.");

            if (request.HourStart.HasValue && request.HourEnd.HasValue)
            {
                if (request.HourStart < 0 || request.HourStart > 23 ||
                    request.HourEnd <= request.HourStart || request.HourEnd > 24)
                    return BadRequest("Invalid hour range.");
            }

            var result = await _revenueService.CalculateRevenueAsync(request);
            return Ok(result);
        }
    }
}
