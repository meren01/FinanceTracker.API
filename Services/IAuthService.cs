using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string? Token, string? Error)> LoginAsync(LoginDto dto);
    }
}