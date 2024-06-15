using AccountBalanceViewerApi.Data;
using AccountBalanceViewerApi.Interfaces;
using AccountsBalanceViewerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountBalanceViewerApi.Repositorys
{
    public class AccountBalanceRepository : IAccountBalanceRepository
    {
        private readonly ApplicationDbContext _context;
        public AccountBalanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccountBalance> CreateAsync(AccountBalance balanceModel)
        {
            await _context.AccountBalances.AddAsync(balanceModel);
            await _context.SaveChangesAsync();

            return balanceModel;
        }

        public async Task<AccountBalance?> GetBalanceForDateAsync(DateTime date)
        {
            return await _context.AccountBalances.FirstOrDefaultAsync(i => i.BalanceDate.Year == date.Year && i.BalanceDate.Month == date.Month);

        }

        public async Task<List<AccountBalance>> GetBalancesForDateAsync(DateTime? date)
        {
            var filterDate = date ?? DateTime.Now;

            return await _context.AccountBalances.Where(i => i.BalanceDate.Year == filterDate.Year && i.BalanceDate.Month == filterDate.Month)
            .OrderByDescending(b => b.BalanceDate)
            .ToListAsync();
        }
    }
}