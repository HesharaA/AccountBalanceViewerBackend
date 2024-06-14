using AccountBalanceViewerApi.Interfaces;
using AccountsBalanceViewerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[Route("api/balance")]
[ApiController]
public class BalancesController : ControllerBase
{
    private readonly IAccountBalanceRepository _repo;

    public BalancesController(IAccountBalanceRepository repo)
    {
        _repo = repo;
    }

    private async Task<IActionResult> GetBalancesForDate(DateTime? date)
    {
        var balances = await _repo.GetBalancesForDateAsync(date);

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
        return await GetBalancesForDate(null);
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
                    var currentBalance = await _repo.GetBalanceForDateAsync(balanceDate);

                    var newAccountBalance = new AccountBalance
                    {
                        AccountName = columns[0],
                        Balance = decimal.Parse(columns[1]),
                        BalanceDate = balanceDate
                    };

                    if (currentBalance == null)
                    {
                        await _repo.CreateAsync(newAccountBalance);
                    }
                    else
                    {
                        return BadRequest("One or more accounts with same balance date already exsists.");
                    }


                }
            }
        }

        return Created();
    }
}
