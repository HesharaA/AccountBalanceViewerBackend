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

    /// <summary> Checks if a account balance is already present in the DB for the given <c><paramref name="date"/></c>.</summary>
    /// <param name="date"> Used as the predicate to search entries.</param>
    /// <returns> true if an entry is found in the DB.</returns>
    private async Task<bool> isAccountBalancePresent(DateTime date)
    {
        AccountBalance? balance = await _repo.GetBalanceForDateAsync(date);

        return balance != null;
    }

    /// <summary> Validates the <c><paramref name="file"/></c> and produces a list of <c>AccountBalance</c>.</summary>
    /// <param name="file"> File that's validated and used to generate a <c>AccountBalance</c> list.</param>
    /// <param name="type"> Used to determine <c>file</c> type provided to validate the <c>file</c> accordingly.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when,
    /// *data is not found within the <c>file</c>.
    /// *The <c>file</c> isn't structured properly (with three colunms).
    /// *<c>file</c> contains account balances that are already added to the DB.
    /// *invalid data found in file <c>file</c>.
    /// </exception>
    /// <returns> Returns a list of <c>AccountBalance</c> that are found within the <c>file</c>.</returns>
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

    /// <summary> Processes <c><paramref name="file"/></c> and adds the resulted account balances to DB.</summary>
    /// <param name="file"> File that's validated and used to generate a <c>AccountBalance</c> list.</param>
    /// <param name="type"> Used to determine <c>file</c> type.</param>
    private async Task<AccountBalance> HandleFile(IFormFile file, FileType type)
    {
        List<AccountBalance> balances = await ProcessFile(file, type);

        foreach (AccountBalance balance in balances)
        {
            await _repo.CreateAsync(balance);
        }

        return balances[0];
    }

    /// <summary> Handles request to get balances for given <c><paramref name="date"/></c>.</summary>        
    /// <param name="date"> Used as the predicate to search entries.</param>       
    [HttpGet]
    public async Task<IActionResult> GetBalances([FromQuery] DateTime? date)

    {
        var balances = await _repo.GetBalancesForDateAsync(date ?? DateTime.Now);

        return Ok(balances);
    }

    /// <summary> Returns all the distinct dates found in the DB.</summary>        
    [HttpGet("distinct")]
    public async Task<IActionResult> GetDistinctBalanceDate()
    {
        var balances = await _repo.GetDistinctBalanceDatesAsync();

        return Ok(balances);
    }
    /// <summary> Handles <c>AccountBalance</c> data entry to DB from given <c><paramref name="file"/></c>.</summary>     
    /// <param name="file"> Used to extract data and add to DB.</param>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadBalanceFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Data not found in file");

        var extension = Path.GetExtension(file.FileName);
        AccountBalance resultDate;
        try
        {
            if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                resultDate = await HandleFile(file, FileType.tab);
            }
            else if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                resultDate = await HandleFile(file, FileType.xl);
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
        return Ok(resultDate);
    }
}
