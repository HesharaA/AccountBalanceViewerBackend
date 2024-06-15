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

        ///<summary> Adds and single <c>AccountBalance</c> entry to the DB.</summary>
        /// <param name="balanceModel"> Model used to enter into the DB.</param>
        /// <returns><c>AccountBalance</c> that was added to the DB.</returns>
        public async Task<AccountBalance> CreateAsync(AccountBalance balanceModel)
        {
            await _context.AccountBalances.AddAsync(balanceModel);
            await _context.SaveChangesAsync();

            return balanceModel;
        }

        ///<summary> Retrieves single <c>AccountBalance</c> from DB.</summary>
        /// <param name="date"> Used as the the predicate to query the DB.</param>
        /// <returns> single <c>AccountBalance</c> from DB using the <c><paramref name="date"/></c>.</returns>
        public async Task<AccountBalance?> GetBalanceForDateAsync(DateTime date)
        {
            return await _context.AccountBalances.FirstOrDefaultAsync(i => i.BalanceDate.Year == date.Year && i.BalanceDate.Month == date.Month);

        }

        ///<summary> Retrieves a list <c>AccountBalance</c> from DB for given <c><paramref name="date"/></c> or current date.</summary>
        /// <param name="date"> Used as the the predicate to query the DB.</param>
        /// <returns> list of <c>AccountBalance</c> from DB using the <c><paramref name="date"/></c> or for current date.</returns>
        public async Task<List<AccountBalance>> GetBalancesForDateAsync(DateTime? date)
        {
            var filterDate = date ?? DateTime.Now;

            return await _context.AccountBalances.Where(i => i.BalanceDate.Year == filterDate.Year && i.BalanceDate.Month == filterDate.Month)
            .OrderByDescending(b => b.BalanceDate)
            .ToListAsync();
        }
    }
}