using System.Threading.Tasks;

namespace FinanceTracker.API.Services
{
    public interface ICurrencyService
    {
        Task<decimal> GetRateAsync(string from, string to);
    }
}
