using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountsBalanceViewerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountsBalanceViewerApi.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {

        }

        public DbSet<AccountBalance> AccountBalances { get; set; }

    }
}