using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Models;
using SmartFinancialTracker.Services;

namespace SmartFinancialTracker;

public partial class AddIncomePage : ContentPage
{
    private readonly D1Service _d1Service;
    private readonly UserService _userService;

    public AddIncomePage(D1Service d1Service, UserService userService)
    {
        InitializeComponent();
        _d1Service = d1Service;
        _userService = userService;
        SourcePicker.SelectedIndex = 0;
    }

    public AddIncomePage() : this(
        IPlatformApplication.Current?.Services.GetRequiredService<D1Service>() ?? new D1Service(),
        IPlatformApplication.Current?.Services.GetRequiredService<UserService>() ?? new UserService())
    {
    }

    private async Task<int> EnsureValidAccountIdAsync(string userId)
    {
        const string findAccountSql = "SELECT AccountId FROM BankAccount WHERE UserId = ? LIMIT 1;";
        var bankResp = await _d1Service.QueryAsync<BankAccount>(findAccountSql, userId);

        if (bankResp?.Success == true && bankResp.Results?.Count > 0)
        {
            return bankResp.Results[0].AccountId;
        }

        // Fixed: Matches exact BankAccount schema (UserId, BankBalance)
        const string insertAccountSql = """
        INSERT INTO BankAccount (UserId, BankBalance)
        VALUES (?, 0);
        """;
        await _d1Service.QueryAsync<object>(insertAccountSql, userId);

        var newBankResp = await _d1Service.QueryAsync<BankAccount>(findAccountSql, userId);
        return newBankResp?.Results?.FirstOrDefault()?.AccountId ?? 1;
    }

    private async Task<int> EnsureValidCategoryIdAsync(string categoryName)
    {
        const string findCatSql = "SELECT CategoryId FROM Category WHERE CategoryName = ? LIMIT 1;";
        var catResp = await _d1Service.QueryAsync<Category>(findCatSql, categoryName);

        if (catResp?.Success == true && catResp.Results?.Count > 0)
        {
            return catResp.Results[0].CategoryId;
        }

        const string insertCatSql = "INSERT INTO Category (CategoryName) VALUES (?);";
        await _d1Service.QueryAsync<object>(insertCatSql, categoryName);

        var newCatResp = await _d1Service.QueryAsync<Category>(findCatSql, categoryName);
        return newCatResp?.Results?.FirstOrDefault()?.CategoryId ?? 1;
    }

    private async void SaveIncomeBtn_Clicked(object sender, EventArgs e)
    {
        var currentUser = _userService.CurrentUser;
        if (currentUser == null)
        {
            await DisplayAlertAsync("Error", "No user logged in.", "OK");
            return;
        }

        string rawAmount = AmountEntry.Text?.Trim().Replace("$", string.Empty) ?? string.Empty;
        if (!decimal.TryParse(rawAmount, out decimal amountDecimal) || amountDecimal <= 0)
        {
            await DisplayAlertAsync("Invalid Amount", "Please enter a valid numeric amount.", "OK");
            return;
        }

        int amountCents = (int)Math.Round(amountDecimal * 100);
        string source = SourcePicker.SelectedItem?.ToString() ?? "Income";
        string description = DescriptionEntry.Text?.Trim() ?? source;

        SaveIncomeBtn.IsEnabled = false;

        try
        {
            int accountId = await EnsureValidAccountIdAsync(currentUser.UserId);
            int categoryId = await EnsureValidCategoryIdAsync("Income");

            var tx = new Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionAmount = amountCents,
                Date = DateTime.UtcNow,
                TransactionType = "Income",
                Description = $"{source}: {description}",
                DetectFraud = false,
                ReceiptImg = string.Empty,
                Status = "Completed",
                UserId = currentUser.UserId,
                CategoryId = categoryId,
                AccountId = accountId
            };

            const string insertSql = """
                INSERT INTO TransactionRecord (TransactionId, TransactionAmount, Date, TransactionType, Description, DetectFraud, ReceiptImg, Status, UserId, CategoryId, AccountId)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                """;

            var result = await _d1Service.QueryAsync<object>(
                insertSql, tx.TransactionId, tx.TransactionAmount, tx.Date.ToString("o"),
                tx.TransactionType, tx.Description, 0, "", tx.Status, tx.UserId, tx.CategoryId, tx.AccountId);

            if (result?.Success == true)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlertAsync("Error", "Failed to save income record.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Save failed: {ex.Message}", "OK");
        }
        finally
        {
            SaveIncomeBtn.IsEnabled = true;
        }
    }

    private async void Back_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}