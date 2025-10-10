using Microsoft.AspNetCore.Mvc;
using FleksProfitAPI.Models;
using FleksProfitAPI.Services;

namespace FleksProfitAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FcrDataController : ControllerBase
    {
        private readonly FcrDataService _fcrService;

        public FcrDataController(FcrDataService fcrService)
        {
            _fcrService = fcrService;
        }
    }
}
