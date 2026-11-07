using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Models;
using SmartFinancialTracker.Services;

namespace SmartFinancialTracker;

public partial class AddExpensePage : ContentPage
{
    private readonly D1Service _d1Service;
    private readonly UserService _userService;
    private List<Category> _categories = new();
    private const int MaxExpenseLimitCents = 20000; // $200.00 limit

    public AddExpensePage(D1Service d1Service, UserService userService)
    {
        InitializeComponent();
        _d1Service = d1Service;
        _userService = userService;
    }

    public AddExpensePage() : this(
        IPlatformApplication.Current?.Services.GetRequiredService<D1Service>() ?? new D1Service(),
        IPlatformApplication.Current?.Services.GetRequiredService<UserService>() ?? new UserService())
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            const string sql = "SELECT * FROM Category;";
            var response = await _d1Service.QueryAsync<Category>(sql);
            if (response?.Success == true && response.Results?.Count > 0)
            {
                _categories = response.Results;
            }
            else
            {
                int defaultCatId = await EnsureValidCategoryIdAsync("General Expense");
                _categories = new List<Category>
                {
                    new() { CategoryId = defaultCatId, CategoryName = "General Expense" }
                };
            }

            CategoryPicker.ItemsSource = _categories;
            if (_categories.Count > 0) CategoryPicker.SelectedIndex = 0;
        }
        catch
        {
            _categories = new List<Category> { new() { CategoryId = 1, CategoryName = "General Expense" } };
            CategoryPicker.ItemsSource = _categories;
            CategoryPicker.SelectedIndex = 0;
        }
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

    private async void SaveExpenseBtn_Clicked(object sender, EventArgs e)
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
            await DisplayAlertAsync("Invalid Amount", "Please enter a valid amount.", "OK");
            return;
        }

        int amountCents = (int)Math.Round(amountDecimal * 100);
        if (amountCents > MaxExpenseLimitCents)
        {
            await DisplayAlertAsync("Limit Exceeded", "Single expense transaction limit is $200.00.", "OK");
            return;
        }

        SaveExpenseBtn.IsEnabled = false;

        try
        {
            int accountId = await EnsureValidAccountIdAsync(currentUser.UserId);
            var selectedCategory = CategoryPicker.SelectedItem as Category;
            int categoryId = selectedCategory?.CategoryId ?? await EnsureValidCategoryIdAsync("General Expense");

            string description = DescriptionEntry.Text?.Trim() ?? selectedCategory?.CategoryName ?? "Expense";

            var tx = new Transaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                TransactionAmount = amountCents,
                Date = DateTime.UtcNow,
                TransactionType = "Expense",
                Description = description,
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
                await DisplayAlertAsync("Error", "Failed to save expense record.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Save failed: {ex.Message}", "OK");
        }
        finally
        {
            SaveExpenseBtn.IsEnabled = true;
        }
    }

    private async void ScanReceipt_Tapped(object sender, EventArgs e)
    {
        await DisplayAlertAsync("AI Scanner", "Opening receipt scanner camera...", "OK");
    }

    private async void Back_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}