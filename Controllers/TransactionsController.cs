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
    [Authorize] // Bu controller’a yalnızca login olmuş kullanıcılar erişebilir
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TransactionsController(AppDbContext db)
        {
            _db = db;
        }

        // JWT’den kullanıcı ID’sini alma
        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? "0");

        // GET: api/transactions -> Tüm işlemleri getir
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var trans = await _db.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category) // Kategori bilgilerini de al
                .OrderByDescending(t => t.Date)
                .Select(t => new {
                    t.Id,
                    t.Amount,
                    t.IsIncome,
                    t.Note,
                    t.Date,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category.Name
                })
                .ToListAsync();

            return Ok(trans);
        }

        // POST: api/transactions -> Yeni işlem ekle
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionDto dto)
        {
            try
            {
                var userId = GetUserId();

                // Kategorinin kullanıcıya ait olup olmadığını kontrol et
                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == userId);
                if (category == null) return BadRequest(new { message = "Kategori bulunamadı veya kullanıcıya ait değil." });

                var t = new Transaction
                {
                    Amount = dto.Amount,
                    IsIncome = dto.IsIncome,
                    Note = dto.Note,
                    Date = dto.Date,
                    CategoryId = dto.CategoryId,
                    UserId = userId
                };

                _db.Transactions.Add(t);
                await _db.SaveChangesAsync();

                // Ekleme sonrası geri dönecek veri
                return Ok(new
                {
                    t.Id,
                    t.Amount,
                    t.IsIncome,
                    t.Note,
                    t.Date,
                    CategoryId = t.CategoryId,
                    CategoryName = category.Name
                });
            }
            catch (Exception ex)
            {
                // Hata loglama
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "Sunucu hatası" });
            }
        }

        // PUT: api/transactions/5 -> İşlem güncelle
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
                t.Date = dto.Date;
                t.CategoryId = dto.CategoryId;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    t.Id,
                    t.Amount,
                    t.IsIncome,
                    t.Note,
                    t.Date,
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

        // DELETE: api/transactions/5 -> İşlem sil
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
