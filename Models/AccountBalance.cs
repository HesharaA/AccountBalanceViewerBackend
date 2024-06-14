using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountsBalanceViewerApi.Models
{
    public class AccountBalance
    {
        public int Id { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime BalanceDate { get; set; }
    }
}