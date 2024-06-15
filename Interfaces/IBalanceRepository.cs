using AccountsBalanceViewerApi.Models;

namespace AccountBalanceViewerApi.Interfaces
{
    public interface IAccountBalanceRepository
    {
        Task<List<AccountBalance>> GetBalancesForDateAsync(DateTime? date);

        Task<AccountBalance?> GetBalanceForDateAsync(DateTime date);

        Task<AccountBalance> CreateAsync(AccountBalance balanceModel);

    }
}