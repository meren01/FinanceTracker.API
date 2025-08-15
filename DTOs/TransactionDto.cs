using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.API.DTOs
{
    public class TransactionDto
    {
        public int? Id { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public bool IsIncome { get; set; }

        public string? Note { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }
}