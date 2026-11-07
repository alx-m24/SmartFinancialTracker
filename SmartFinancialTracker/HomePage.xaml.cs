using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Models;
using SmartFinancialTracker.Services;

namespace SmartFinancialTracker;

public partial class HomePage : ContentPage
{
    private readonly D1Service _d1Service;
    private readonly UserService _userService;

    public HomePage(D1Service d1Service, UserService userService)
    {
        InitializeComponent();
        _d1Service = d1Service;
        _userService = userService;
    }

    public HomePage() : this(
        IPlatformApplication.Current?.Services.GetRequiredService<D1Service>() ?? new D1Service(),
        IPlatformApplication.Current?.Services.GetRequiredService<UserService>() ?? new UserService())
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        var currentUser = _userService.CurrentUser;
        if (currentUser == null) return;

        // 1. Render User Header
        string username = string.IsNullOrWhiteSpace(currentUser.Username) ? "Student" : currentUser.Username;
        GreetingLabel.Text = $"Hello, {username}! 👋";
        UserInitialsLabel.Text = username.Length >= 2 ? username[..2].ToUpper() : "ST";
        CurrentMonthLabel.Text = DateTime.UtcNow.ToString("MMMM yyyy");

        // 2. Query Bank Account Details & Base Balance
        const string balanceSql = "SELECT * FROM BankAccount WHERE UserId = ? LIMIT 1;";
        var bankResponse = await _d1Service.QueryAsync<BankAccount>(balanceSql, currentUser.UserId);

        int baseBankBalanceCents = 0;
        if (bankResponse?.Success == true && bankResponse.Results?.Count > 0)
        {
            var bankAccount = bankResponse.Results[0];
            baseBankBalanceCents = bankAccount.BankBalance;
            string bankName = string.IsNullOrWhiteSpace(bankAccount.BankName) ? "Primary Account" : bankAccount.BankName;
            string accountType = string.IsNullOrWhiteSpace(bankAccount.AccountType) ? "Checking" : bankAccount.AccountType;
            BankDetailsLabel.Text = $"{bankName} · {accountType}";
        }
        else
        {
            BankDetailsLabel.Text = "No Linked Account";
        }

        // 3. Query Categories for Name Lookup
        var categoriesDict = new Dictionary<int, string>();
        const string categoriesSql = "SELECT * FROM Category;";
        var categoryResponse = await _d1Service.QueryAsync<Category>(categoriesSql);
        if (categoryResponse?.Success == true && categoryResponse.Results != null)
        {
            foreach (var cat in categoryResponse.Results)
            {
                categoriesDict[cat.CategoryId] = cat.CategoryName;
            }
        }

        // 4. Query Monthly Budget Limit
        const string budgetSql = "SELECT * FROM MonthlyBudget WHERE UserId = ? LIMIT 1;";
        var budgetResponse = await _d1Service.QueryAsync<MonthlyBudget>(budgetSql, currentUser.UserId);

        int budgetLimitCents = 100000; // Default $1,000.00 budget limit fallback
        if (budgetResponse?.Success == true && budgetResponse.Results?.Count > 0)
        {
            budgetLimitCents = budgetResponse.Results[0].BudgetAmount;
        }

        // 5. Query Transactions & Calculate Real-Time Net Balance
        const string transactionsSql = "SELECT * FROM TransactionRecord WHERE UserId = ? ORDER BY Date DESC;";
        var txResponse = await _d1Service.QueryAsync<Transaction>(transactionsSql, currentUser.UserId);

        int totalIncomeCents = 0;
        int totalExpenseCents = 0;
        var displayList = new List<TransactionDisplayModel>();

        if (txResponse?.Success == true && txResponse.Results != null)
        {
            foreach (var tx in txResponse.Results)
            {
                bool isIncome = tx.TransactionType.Equals("Income", StringComparison.OrdinalIgnoreCase);
                if (isIncome)
                {
                    totalIncomeCents += tx.TransactionAmount;
                }
                else
                {
                    totalExpenseCents += tx.TransactionAmount;
                }

                // Get category name lookup or fallback
                string categoryName = categoriesDict.TryGetValue(tx.CategoryId, out string? catName) ? catName : (isIncome ? "Income" : "General");

                // Populate top 10 items for dashboard preview
                if (displayList.Count < 10)
                {
                    displayList.Add(new TransactionDisplayModel
                    {
                        TransactionId = tx.TransactionId,
                        Description = string.IsNullOrWhiteSpace(tx.Description) ? tx.TransactionType : tx.Description,
                        CategoryName = categoryName,
                        CategoryIcon = isIncome ? "💵" : GetIconForDescription(tx.Description),
                        DisplayDate = tx.Date.ToString("MMM dd"),
                        FormattedAmount = $"{(isIncome ? "+" : "-")}${(tx.TransactionAmount / 100.0):N2}",
                        AmountColor = isIncome ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444")
                    });
                }
            }
        }

        // Net live balance = Base Balance + Total Income - Total Expenses
        int liveBalanceCents = baseBankBalanceCents + totalIncomeCents - totalExpenseCents;

        TotalBalanceLabel.Text = $"${(liveBalanceCents / 100.0):N2}";
        MonthlyIncomeLabel.Text = $"${(totalIncomeCents / 100.0):N2}";
        MonthlySpendingLabel.Text = $"${(totalExpenseCents / 100.0):N2}";

        // 6. Calculate Budget Progress
        double progressRatio = budgetLimitCents > 0 ? (double)totalExpenseCents / budgetLimitCents : 0.0;
        double clampedProgress = Math.Min(progressRatio, 1.0);
        int percentInt = (int)(progressRatio * 100);

        BudgetProgressBar.Progress = clampedProgress;
        BudgetPercentLabel.Text = $"{percentInt}%";
        BudgetProgressTextLabel.Text = $"${(totalExpenseCents / 100.0):N0} / ${(budgetLimitCents / 100.0):N0} spent";

        // 7. Threshold Warning Alert (Show alert if spending >= 80% of budget)
        if (percentInt >= 80)
        {
            BudgetAlertBorder.IsVisible = true;
            AlertTextLabel.Text = $"Alert: You have reached {percentInt}% of your monthly spending limit!";
        }
        else
        {
            BudgetAlertBorder.IsVisible = false;
        }

        // 8. Bind Recent Transactions Collection
        RecentTransactionsListView.ItemsSource = displayList;
    }

    private string GetIconForDescription(string description)
    {
        string desc = description.ToLower();
        if (desc.Contains("fresh") || desc.Contains("market") || desc.Contains("grocery") || desc.Contains("food")) return "🛒";
        if (desc.Contains("rent") || desc.Contains("accommodation") || desc.Contains("housing")) return "🏠";
        if (desc.Contains("coffee") || desc.Contains("cafe") || desc.Contains("starbucks")) return "☕";
        if (desc.Contains("book") || desc.Contains("tuition") || desc.Contains("school")) return "📚";
        if (desc.Contains("salary") || desc.Contains("paycheck") || desc.Contains("refund")) return "💵";
        return "💳";
    }

    private async void AddTransactionBtn_Clicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("New Transaction", "Cancel", null, "💸 Add Expense", "💰 Add Income");

        if (action == "💸 Add Expense")
        {
            await Shell.Current.GoToAsync(nameof(AddExpensePage));
        }
        else if (action == "💰 Add Income")
        {
            await Shell.Current.GoToAsync(nameof(AddIncomePage));
        }
    }
}