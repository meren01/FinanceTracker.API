using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Data;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Tüm kullanıcıları listeleme
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    IsAdmin = u.Role == "Admin"
                })
                .ToListAsync();

            return Ok(users);
        }

        // Belirli bir kullanıcının detaylarını getirme
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    IsAdmin = u.Role == "Admin"
                })
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            return Ok(user);
        }

        // Admin rolü atama/kaldırma
        [HttpPut("users/{id}/admin-status")]
        public async Task<IActionResult> UpdateAdminStatus(int id, [FromBody] AdminStatusUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // Kullanıcının kendi admin statüsünü değiştirmesini engelleme
            if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id.ToString())
                return BadRequest("Kendi admin statünüzü değiştiremezsiniz.");

            user.Role = dto.IsAdmin ? "Admin" : "User";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Kullanıcı {(dto.IsAdmin ? "admin yapıldı" : "admin yetkisi kaldırıldı")}.",
                isAdmin = dto.IsAdmin
            });
        }

        public class AdminStatusUpdateDto
        {
            public bool IsAdmin { get; set; }
        }


        // Kullanıcı silme
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // Kullanıcının kendisini silmesini engelleme
            if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id.ToString())
                return BadRequest("Kendinizi silemezsiniz.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kullanıcı başarıyla silindi." });
        }

       
     
    
    }
}
