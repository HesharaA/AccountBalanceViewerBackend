using AccountsBalanceViewerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountBalanceViewerApi.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {

        }

        public DbSet<AccountBalance> AccountBalances { get; set; }

    }
}