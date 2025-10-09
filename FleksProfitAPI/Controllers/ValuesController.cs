using FleksProfitAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FleksProfitAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RevenueController : ControllerBase
    {
        private readonly FcrRevenueService _revenueService;

        public RevenueController(FcrRevenueService revenueService)
        {
            _revenueService = revenueService;
        }

        // GET /api/revenue?capacityMW=0.03&start=2024-01-01&end=2024-01-31
        [HttpGet]
        public async Task<IActionResult> GetRevenue([FromQuery] double capacityMW, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (capacityMW <= 0) return BadRequest("Capacity must be > 0.");
            if (start >= end) return BadRequest("Start must be before end.");

            var revenue = await _revenueService.CalculateRevenueAsync(capacityMW, start, end);
            return Ok(new { CapacityMW = capacityMW, Start = start, End = end, RevenueDKK = revenue });
        }

        // GET /api/revenue/hours?capacityMW=0.03&start=2024-01-01&end=2024-01-31&hourStart=12&hourEnd=16
        [HttpGet("hours")]
        public async Task<IActionResult> GetRevenueForHours([FromQuery] double capacityMW, [FromQuery] DateTime start, [FromQuery] DateTime end,
            [FromQuery] int hourStart, [FromQuery] int hourEnd)
        {
            if (capacityMW <= 0) return BadRequest("Capacity must be > 0.");
            if (start >= end) return BadRequest("Start must be before end.");
            if (hourStart < 0 || hourStart > 23 || hourEnd <= hourStart || hourEnd > 24) return BadRequest("Invalid hours.");

            var revenue = await _revenueService.CalculateRevenueForHoursAsync(capacityMW, start, end, hourStart, hourEnd);
            return Ok(new { CapacityMW = capacityMW, Start = start, End = end, HourStart = hourStart, HourEnd = hourEnd, RevenueDKK = revenue });
        }
    }
}
