using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace FinanceTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICurrencyService _currencyService;

        public TransactionsController(AppDbContext db, ICurrencyService currencyService)
        {
            _db = db;
            _currencyService = currencyService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? "0");

        // ======================================================================
        //                               GET ALL
        // ======================================================================
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string period = "3m",
            [FromQuery] int? categoryId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();

            var query = _db.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .AsQueryable();

            DateTime now = DateTime.Now;
            DateTime startDate = period switch
            {
                "1m" => now.AddMonths(-1),
                "3m" => now.AddMonths(-3),
                "6m" => now.AddMonths(-6),
                "9m" => now.AddMonths(-9),
                "12m" => now.AddMonths(-12),
                _ => DateTime.MinValue
            };

            query = query.Where(t => t.Date >= startDate);

            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId.Value);

            var totalRecords = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
                    t.Currency,
                    t.AmountInTRY,
                    t.IsIncome,
                    t.Note,
                    Date = t.Date.ToLocalTime(),
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category.Name
                })
                .ToListAsync();

            return Ok(new
            {
                totalRecords,
                page,
                pageSize,
                transactions
            });
        }

        // ======================================================================
        //                                 CREATE
        // ======================================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionDto dto)
        {
            try
            {
                var userId = GetUserId();

                var category = await _db.Categories
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

                if (category == null)
                    return BadRequest(new { message = "Kategori bulunamadı veya kullanıcıya ait değil." });

                decimal rate = 1;
                if (dto.Currency != "TRY")
                    rate = await _currencyService.GetRateAsync(dto.Currency, "TRY");

                decimal amountTL = dto.Amount * rate;

                var t = new Transaction
                {
                    Amount = dto.Amount,
                    Currency = dto.Currency,
                    AmountInTRY = amountTL,
                    IsIncome = dto.IsIncome,
                    Note = dto.Note,
                    Date = dto.Date.ToLocalTime(),
                    CategoryId = dto.CategoryId,
                    UserId = userId
                };

                _db.Transactions.Add(t);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    t.Id,
                    t.Amount,
                    t.Currency,
                    t.AmountInTRY,
                    t.IsIncome,
                    t.Note,
                    Date = t.Date.ToLocalTime(),
                    CategoryId = t.CategoryId,
                    CategoryName = category.Name
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "Sunucu hatası" });
            }
        }

        // ======================================================================
        //                                 UPDATE
        // ======================================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TransactionDto dto)
        {
            try
            {
                var userId = GetUserId();

                var t = await _db.Transactions
                    .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId);

                if (t == null) return NotFound();

                var category = await _db.Categories
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

                if (category == null)
                    return BadRequest(new { message = "Kategori bulunamadı veya kullanıcıya ait değil." });

                decimal rate = 1;
                if (dto.Currency != "TRY")
                    rate = await _currencyService.GetRateAsync(dto.Currency, "TRY");

                decimal amountTL = dto.Amount * rate;

                t.Amount = dto.Amount;
                t.Currency = dto.Currency;
                t.AmountInTRY = amountTL;
                t.IsIncome = dto.IsIncome;
                t.Note = dto.Note;
                t.Date = dto.Date.ToLocalTime();
                t.CategoryId = dto.CategoryId;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    t.Id,
                    t.Amount,
                    t.Currency,
                    t.AmountInTRY,
                    t.IsIncome,
                    t.Note,
                    Date = t.Date.ToLocalTime(),
                    CategoryId = t.CategoryId,
                    CategoryName = category.Name
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "Sunucu hatası" });
            }
        }

        // ======================================================================
        //                                 DELETE
        // ======================================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetUserId();

                var t = await _db.Transactions
                    .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId);

                if (t == null)
                    return NotFound(new { message = "Silinecek işlem bulunamadı." });

                _db.Transactions.Remove(t);
                await _db.SaveChangesAsync();

                return Ok(new { message = "İşlem başarıyla silindi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "Sunucu hatası" });
            }
        }


        // ======================================================================
        //                        💥 DÖVİZ TEST ENDPOINTİ
        // ======================================================================
        [HttpGet("test")]
        [AllowAnonymous]
        public async Task<IActionResult> Test()
        {
            try
            {
                var usd = await _currencyService.GetRateAsync("USD", "TRY");
                var eur = await _currencyService.GetRateAsync("EUR", "TRY");
                var gbp = await _currencyService.GetRateAsync("GBP", "TRY");

                return Ok(new { USD = usd, EUR = eur, GBP = gbp });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
