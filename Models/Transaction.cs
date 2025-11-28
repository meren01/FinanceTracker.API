using FinanceTracker.API.Models;
using System.ComponentModel.DataAnnotations;

public class Transaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "TRY";

    [Required]
    public decimal AmountInTRY { get; set; }

    [Required]
    public bool IsIncome { get; set; }

    public string? Note { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }
}
