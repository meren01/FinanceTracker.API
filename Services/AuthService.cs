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
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                    return (false, "Invalid registration data.");

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
            catch (Exception ex)
            {
                return (false, $"Internal error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Token, string? Error)> LoginAsync(LoginDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                    return (false, null, "Invalid login data.");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
                if (user == null) return (false, null, "Invalid credentials.");

                var verified = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
                if (!verified) return (false, null, "Invalid credentials.");

                var token = GenerateToken(user);
                return (true, token, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Internal error: {ex.Message}");
            }
        }

        private string GenerateToken(User user)
        {
            try
            {
                var jwtSection = _config.GetSection("Jwt");

                var keyString = jwtSection.GetValue<string>("Key");
                var issuer = jwtSection.GetValue<string>("Issuer");
                var audience = jwtSection.GetValue<string>("Audience");
                var expireMinutes = jwtSection.GetValue<int?>("ExpireMinutes") ?? 60;

                if (string.IsNullOrEmpty(keyString) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                    throw new Exception("JWT configuration is invalid.");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role), // Rol bilgisini ekliyoruz
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                throw new Exception($"Token generation failed: {ex.Message}");
            }
        }
    }
}
