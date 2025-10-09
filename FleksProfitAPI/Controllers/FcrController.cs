using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] DateTime? start = null,
            [FromQuery] DateTime? end = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 500)
        {
            var result = await _fcrService.GetPagedAsync(start, end, page, pageSize);
            return Ok(result);
        }
    }
}
