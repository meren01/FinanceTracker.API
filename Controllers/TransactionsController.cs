using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
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

        public TransactionsController(AppDbContext db)
        {
            _db = db;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? "0");

        // ✅ Filtreleme + Pagination
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string period = "3m",
            [FromQuery] int? categoryId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();

            // Kullanıcıya ait işlemler
            var query = _db.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .AsQueryable();

            // 🔹 Tarih filtreleme (Yerel zaman kullan)
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

            // 🔹 Kategori filtreleme
            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            // 🔹 Toplam kayıt sayısı
            var totalRecords = await query.CountAsync();

            // 🔹 Sayfalama ve sıralama
            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
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

        // ✅ Yeni işlem ekleme
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionDto dto)
        {
            try
            {
                var userId = GetUserId();

                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == userId);
                if (category == null)
                    return BadRequest(new { message = "Kategori bulunamadı veya kullanıcıya ait değil." });

                var t = new Transaction
                {
                    Amount = dto.Amount,
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

        // ✅ İşlem güncelleme
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TransactionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var t = await _db.Transactions.FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId);
                if (t == null) return NotFound();

                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == userId);
                if (category == null) return BadRequest(new { message = "Kategori bulunamadı veya kullanıcıya ait değil." });

                t.Amount = dto.Amount;
                t.IsIncome = dto.IsIncome;
                t.Note = dto.Note;
                t.Date = dto.Date.ToLocalTime();
                t.CategoryId = dto.CategoryId;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    t.Id,
                    t.Amount,
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

        // ✅ İşlem silme
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetUserId();
                var t = await _db.Transactions.FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId);
                if (t == null) return NotFound();

                _db.Transactions.Remove(t);
                await _db.SaveChangesAsync();

                return Ok(new { message = "İşlem silindi" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "Sunucu hatası" });
            }
        }
    }
}
