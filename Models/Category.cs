using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.API.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        // Owner
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        public ICollection<Transaction>? Transactions { get; set; }
    }
}