using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.API.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        public ICollection<Category>? Categories { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
    }
}