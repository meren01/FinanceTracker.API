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

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            var userId = GetUserId();

            var transactions = await _db.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            var balance = totalIncome - totalExpense;

            var summary = new DashboardSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = balance
            };

            return Ok(summary);
        }
    }
}
