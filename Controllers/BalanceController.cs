using AccountBalanceViewerApi.Interfaces;
using AccountsBalanceViewerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;

[Route("api/balance")]
[ApiController]
public class BalancesController : ControllerBase
{
    private readonly IAccountBalanceRepository _repo;

    public BalancesController(IAccountBalanceRepository repo)
    {
        _repo = repo;
    }

    private async Task<bool> isAccountBalancePresent(DateTime date)
    {
        AccountBalance? balance = await _repo.GetBalanceForDateAsync(date);

        return balance != null;
    }

    private async Task<List<AccountBalance>> ProcessTextFile(IFormFile file)
    {
        List<AccountBalance> balances = [];

        using (var stream = new StreamReader(file.OpenReadStream()))
        {
            int lineCount = 0;
            string? line = await stream.ReadLineAsync();
            while (line != null)
            {
                var commaSeparated = line!.Split(',');
                var columns = line!.Split('\t');

                if (lineCount == 6 && columns.Length == 1 && columns[0].IsNullOrEmpty())
                {
                    line = await stream.ReadLineAsync();
                    continue;
                }
                else if (lineCount > 5 || columns.Length != 3 || commaSeparated.Length > columns.Length)
                {
                    throw new InvalidOperationException("The file is not a valid tab-separated file.");
                }

                if (DateTime.TryParse(columns[2], out var balanceDate))
                {
                    if (!await isAccountBalancePresent(balanceDate))
                    {
                        var newAccountBalance = new AccountBalance
                        {
                            AccountName = columns[0],
                            Balance = decimal.Parse(columns[1]),
                            BalanceDate = balanceDate
                        };

                        balances.Add(newAccountBalance);
                    }
                    else
                    {
                        throw new InvalidOperationException("Files contains account balance that is already present");
                    }
                }

                line = await stream.ReadLineAsync();
                lineCount++;
            }
        }

        return balances;

    }

    private async Task HandleTabSeparatedFile(IFormFile file)
    {
        List<AccountBalance> balances = await ProcessTextFile(file);

        foreach (AccountBalance balance in balances)
        {
            await _repo.CreateAsync(balance);
        }
    }

    private async Task HandleExcelFile(IFormFile file)
    {
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    return;

                var rowCount = worksheet.Dimension.Rows;
                for (int row = 2; row <= rowCount; row++)
                {
                    var accountName = worksheet.Cells[row, 1].Text;
                    var balance = decimal.Parse(worksheet.Cells[row, 2].Text);
                    var balanceDate = DateTime.Parse(worksheet.Cells[row, 3].Text);

                    var accountBalance = new AccountBalance
                    {
                        AccountName = accountName,
                        Balance = balance,
                        BalanceDate = balanceDate
                    };
                    await _repo.CreateAsync(accountBalance);
                }
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBalances([FromQuery] DateTime? date)
    {
        var balances = await _repo.GetBalancesForDateAsync(date ?? DateTime.Now);

        if (balances.IsNullOrEmpty())
        {
            return NoContent();
        }

        return Ok(balances);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadBalanceFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Data not found in file");

        var extension = Path.GetExtension(file.FileName);
        if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await HandleTabSeparatedFile(file);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        else if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            await HandleExcelFile(file);
        }
        else
        {
            return BadRequest("Unsupported file type");
        }

        return Ok();
    }
}
