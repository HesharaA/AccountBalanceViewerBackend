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

    enum FileType
    {
        tab,
        xl,
    }

    public BalancesController(IAccountBalanceRepository repo)
    {
        _repo = repo;
    }

    private async Task<bool> isAccountBalancePresent(DateTime date)
    {
        AccountBalance? balance = await _repo.GetBalanceForDateAsync(date);

        return balance != null;
    }

    private async Task<List<AccountBalance>> ProcessFile(IFormFile file, FileType type)
    {
        List<AccountBalance> balances = [];

        switch (type)
        {
            case FileType.tab:
                {

                    using var stream = new StreamReader(file.OpenReadStream());
                    int lineCount = 0;
                    string? line = await stream.ReadLineAsync();
                    while (line != null)
                    {

                        Console.WriteLine("Line" + lineCount);
                        var columns = line!.Split('\t');

                        if (lineCount == 0 || lineCount > 5)
                        {
                            line = await stream.ReadLineAsync();
                            lineCount++;
                            continue;
                        }

                        if (columns.Length != 3)
                        {
                            throw new InvalidOperationException("The file is not a valid tab-separated file.");
                        }

                        if (DateTime.TryParse(columns[2], out var balanceDate) && decimal.TryParse(columns[1], out var balance))
                        {
                            if (!await isAccountBalancePresent(balanceDate))
                            {

                                var newAccountBalance = new AccountBalance
                                {
                                    AccountName = columns[0].ToString(),
                                    Balance = balance,
                                    BalanceDate = balanceDate
                                };

                                balances.Add(newAccountBalance);
                            }
                            else
                            {

                                throw new InvalidOperationException("Files contains account balance that is already present");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid data found in file");
                        }

                        line = await stream.ReadLineAsync();
                        lineCount++;
                    }
                }
                break;
            case FileType.xl:
                {
                    using var stream = new MemoryStream();
                    await file.CopyToAsync(stream);
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("Data not found in file");

                    var rowCount = worksheet.Dimension.Rows;
                    if (worksheet.Dimension.Columns == 3)
                    {
                        for (int row = 2; row <= rowCount; row++)
                        {
                            if (DateTime.TryParse(worksheet.Cells[row, 3].Text, out var balanceDate) && decimal.TryParse(worksheet.Cells[row, 2].Text, out var balance))
                            {
                                if (!await isAccountBalancePresent(balanceDate))
                                {
                                    var newAccountBalance = new AccountBalance
                                    {
                                        AccountName = worksheet.Cells[row, 1].Text.ToString(),
                                        Balance = balance,
                                        BalanceDate = balanceDate
                                    };

                                    balances.Add(newAccountBalance);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Files contains account balance that is already present");
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("Invalid data found in file");
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("The file is not a valid excel file.");

                    }
                }
                break;
            default: throw new InvalidOperationException("Unsuported file type");
        }

        return balances;

    }

    private async Task HandleFile(IFormFile file, FileType type)
    {
        List<AccountBalance> balances = await ProcessFile(file, type);

        foreach (AccountBalance balance in balances)
        {
            await _repo.CreateAsync(balance);
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
        try
        {
            if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                await HandleFile(file, FileType.tab);
            }
            else if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                await HandleFile(file, FileType.xl);
            }
            else
            {
                return BadRequest("Unsupported file type");
            }
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        return Ok();
    }
}
