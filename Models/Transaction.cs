using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.API.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public bool IsIncome { get; set; } // true => income, false => expense

        public string? Note { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        // Category relation
        [Required]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Owner reference for easy filtering
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}