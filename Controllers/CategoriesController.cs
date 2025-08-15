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
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? "0");

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var categories = await _db.Categories
                .Where(c => c.UserId == userId)
                .Select(c => new { c.Id, c.Name, c.Description })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/categories/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var category = await _db.Categories
                .Where(c => c.UserId == userId && c.Id == id)
                .Select(c => new { c.Id, c.Name, c.Description })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            var userId = GetUserId();

            if (await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name.ToLower() == dto.Name.ToLower()))
                return BadRequest(new { message = "Category with same name already exists." });

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                UserId = userId
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        // PUT: api/categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryDto dto)
        {
            var userId = GetUserId();

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (category == null)
                return NotFound();

            // Aynı isimde başka kategori var mı kontrolü
            if (await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name.ToLower() == dto.Name.ToLower() && c.Id != id))
                return BadRequest(new { message = "Category with same name already exists." });

            category.Name = dto.Name;
            category.Description = dto.Description;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (category == null)
                return NotFound();

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
