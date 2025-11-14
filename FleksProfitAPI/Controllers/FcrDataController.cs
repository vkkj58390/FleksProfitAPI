using Microsoft.AspNetCore.Mvc;
using FleksProfitAPI.Services;
using FleksProfitAPI.Data;

namespace FleksProfitAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FcrDataController : ControllerBase
    {
        private readonly FcrDataService _fcrService;
        private readonly QuestDbRepository _repo;

        public FcrDataController(FcrDataService fcrService, QuestDbRepository repo)
        {
            _fcrService = fcrService;
            _repo = repo;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromQuery] DateTime? start, [FromQuery] DateTime? end, CancellationToken ct)
        {
            // Ensure the table exists
            await _repo.EnsureTableExistsAsync(ct);

            // Default to last 2 days if no dates provided
            var e = end ?? DateTime.UtcNow.Date;
            var s = start ?? e.AddDays(-2);


            var inserted = await _fcrService.SyncFcrDataAsync(s, e, ct);
            return Ok(new { inserted, start = s, end = e });
        }

        [HttpGet("count")]
        public async Task<IActionResult> Count([FromQuery] DateTime start, [FromQuery] DateTime end, CancellationToken ct)
        {
            var rows = await _repo.GetFcrRecordsAsync(
                DateTime.SpecifyKind(start, DateTimeKind.Utc),
                DateTime.SpecifyKind(end, DateTimeKind.Utc),
                ct);

            return Ok(new { count = rows.Count });
        }
    }
}
