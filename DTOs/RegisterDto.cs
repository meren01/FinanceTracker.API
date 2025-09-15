using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.API.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(User|Admin)$", ErrorMessage = "Role must be User or Admin.")]
        public string Role { get; set; } = "User";
    }
}

