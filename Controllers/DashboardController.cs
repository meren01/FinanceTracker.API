using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FinanceTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? "0");

        // ============================
        // 1) SUMMARY (Toplam gelir/gider)
        // ============================
        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            var userId = GetUserId();

            var transactions = await _db.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            decimal totalIncome = transactions
                .Where(t => t.IsIncome)
                .Sum(t => t.AmountInTRY);

            decimal totalExpense = transactions
                .Where(t => !t.IsIncome)
                .Sum(t => t.AmountInTRY);

            var dto = new DashboardSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = totalIncome - totalExpense,
                Currency = "TRY"
            };

            return Ok(dto);
        }

        // ============================
        // 2) CATEGORY SUMMARY (Kategoriler bazlı gelir/gider)
        // ============================
        [HttpGet("category-summary")]
        public async Task<ActionResult<DashboardCategorySummaryDto>> GetCategorySummary()
        {
            var userId = GetUserId();

            var transactions = await _db.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .ToListAsync();

            var incomeList = transactions
                .Where(t => t.IsIncome)
                .GroupBy(t => t.Category.Name)
                .Select(g => new CategoryTotalDto
                {
                    CategoryName = g.Key,
                    Total = g.Sum(x => x.AmountInTRY)
                })
                .ToList();

            var expenseList = transactions
                .Where(t => !t.IsIncome)
                .GroupBy(t => t.Category.Name)
                .Select(g => new CategoryTotalDto
                {
                    CategoryName = g.Key,
                    Total = g.Sum(x => x.AmountInTRY)
                })
                .ToList();

            var dto = new DashboardCategorySummaryDto
            {
                Income = incomeList,
                Expense = expenseList,
                Currency = "TRY"
            };

            return Ok(dto);
        }

        // ======================================
        // 3) MONTHLY (Linechart / histogram için)
        // ======================================
        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthly()
        {
            var userId = GetUserId();

            var transactions = await _db.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var result = new List<object>();

            var grouped = transactions
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month);

            foreach (var g in grouped)
            {
                result.Add(new
                {
                    Month = $"{g.Key.Month:D2}/{g.Key.Year}",
                    Income = g.Where(x => x.IsIncome).Sum(x => x.AmountInTRY),
                    Expense = g.Where(x => !x.IsIncome).Sum(x => x.AmountInTRY)
                });
            }

            return Ok(result);
        }

        // ============================
        // 4) TEST endpoint (isteğe bağlı)
        // ============================
        [AllowAnonymous]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Dashboard API çalışıyor" });
        }
    }
}

