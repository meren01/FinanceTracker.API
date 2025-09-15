namespace FinanceTracker.API.DTOs
{
    public class DashboardCategorySummaryDto
    {
        public List<CategoryTotalDto> Income { get; set; } = new();
        public List<CategoryTotalDto> Expense { get; set; } = new();
    }
}