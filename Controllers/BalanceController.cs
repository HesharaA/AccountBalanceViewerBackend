using AccountBalanceViewerApi.Data;
using AccountsBalanceViewerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[Route("api/balance")]
[ApiController]
public class BalancesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BalancesController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<IActionResult> GetBalancesForDate(DateTime date)
    {
        DateTime filterDate = date;

        var balances = await _context.AccountBalances.Where(i => i.BalanceDate.Year == filterDate.Year && i.BalanceDate.Month == filterDate.Month)
            .OrderByDescending(b => b.BalanceDate)
            .ToListAsync();

        if (balances.IsNullOrEmpty())
        {
            return NoContent();
        }

        return Ok(balances);
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> GetBalances([FromRoute] DateTime date)
    {
        return await GetBalancesForDate(date);
    }

    [HttpGet]
    public async Task<IActionResult> GetBalances()
    {
        return await GetBalancesForDate(DateTime.Now);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadBalanceFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        using (var stream = new StreamReader(file.OpenReadStream()))
        {
            while (stream.Peek() >= 0)
            {
                var line = await stream.ReadLineAsync();
                var columns = line.Split('\t');

                if (columns.Length == 3 && DateTime.TryParse(columns[2], out var balanceDate))
                {
                    var currentBalance = await _context.AccountBalances.FirstAsync(i => i.BalanceDate.Year == balanceDate.Year && i.BalanceDate.Month == balanceDate.Month);

                    var newAccountBalance = new AccountBalance
                    {
                        AccountName = columns[0],
                        Balance = decimal.Parse(columns[1]),
                        BalanceDate = balanceDate
                    };

                    if (currentBalance == null)
                    {
                        _context.AccountBalances.Add(newAccountBalance);
                    }
                    else
                    {
                        return BadRequest("One or more accounts with same balance date already exsists.");
                    }


                }
            }
        }

        await _context.SaveChangesAsync();
        return Created();
    }
}
