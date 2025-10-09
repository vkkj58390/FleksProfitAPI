using Microsoft.AspNetCore.Mvc;
using FleksProfitAPI.Models;
using FleksProfitAPI.Services;

namespace FleksProfitAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FcrController : ControllerBase
    {
        private readonly FcrService _fcrService;

        public FcrController(FcrService fcrService)
        {
            _fcrService = fcrService;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateRevenue([FromBody] RevenueRequest request)
        {
            var result = await _fcrService.CalculateMonthlyRevenueAsync(request);
            return Ok(result);
        }
    }
}
