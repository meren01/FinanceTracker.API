using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ---------------- Register ----------------
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, error) = await _authService.RegisterAsync(dto);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { message = "User registered successfully" });
        }

        // ---------------- Login ----------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, token, error) = await _authService.LoginAsync(dto);

            if (!success)
                return Unauthorized(new { message = error });

            return Ok(new { token });
        }
    }
}
