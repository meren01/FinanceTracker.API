using FinanceTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        // /api/currency/latest
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest()
        {
            try
            {
                var usd = await _currencyService.GetRateAsync("USD", "TRY");
                var eur = await _currencyService.GetRateAsync("EUR", "TRY");
                var gbp = await _currencyService.GetRateAsync("GBP", "TRY");

                return Ok(new
                {
                    USD = usd,
                    EUR = eur,
                    GBP = gbp
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kur alınamadı", error = ex.Message });
            }
        }
    }
}

