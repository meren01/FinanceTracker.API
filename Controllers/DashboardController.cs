using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
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

        // Toplam gelir, gider, bakiye
        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            var userId = GetUserId();
            var transactions = await _db.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);

            return Ok(new DashboardSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = totalIncome - totalExpense
            });
        }

        // Kategori bazlı gelir/gider
        [HttpGet("category-summary")]
        public async Task<ActionResult<DashboardCategorySummaryDto>> GetCategorySummary()
        {
            var userId = GetUserId();
            var transactions = await _db.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .ToListAsync();

            var incomeSummary = transactions
                .Where(t => t.IsIncome)
                .GroupBy(t => t.Category?.Name ?? "Bilinmeyen")
                .Select(g => new CategoryTotalDto { CategoryName = g.Key, Total = g.Sum(t => t.Amount) })
                .ToList();

            var expenseSummary = transactions
                .Where(t => !t.IsIncome)
                .GroupBy(t => t.Category?.Name ?? "Bilinmeyen")
                .Select(g => new CategoryTotalDto { CategoryName = g.Key, Total = g.Sum(t => t.Amount) })
                .ToList();

            return Ok(new DashboardCategorySummaryDto
            {
                Income = incomeSummary,
                Expense = expenseSummary
            });
        }
    }
}
