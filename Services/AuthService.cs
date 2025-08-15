using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinanceTracker.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(RegisterDto dto)
        {
            // Already exists?
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                return (false, "Email already registered.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Success, string? Token, string? Error)> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null) return (false, null, "Invalid credentials.");

            var verified = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!verified) return (false, null, "Invalid credentials.");

            var token = GenerateToken(user);

            return (true, token, null);
        }

        private string GenerateToken(User user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection.GetValue<string>("Key")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expires = DateTime.UtcNow.AddMinutes(jwtSection.GetValue<int>("ExpireMinutes"));

            var token = new JwtSecurityToken(
                issuer: jwtSection.GetValue<string>("Issuer"),
                audience: jwtSection.GetValue<string>("Audience"),
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}