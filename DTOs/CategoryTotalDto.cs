namespace FinanceTracker.API.DTOs
{
    public class CategoryTotalDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}