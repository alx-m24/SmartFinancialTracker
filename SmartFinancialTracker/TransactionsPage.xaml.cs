using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Models;
using SmartFinancialTracker.Services;

namespace SmartFinancialTracker;

public partial class TransactionsPage : ContentPage
{
    private readonly D1Service _d1Service;
    private readonly UserService _userService;

    private List<TransactionDisplayModel> _allTransactions = new();
    private string _currentFilter = "All";

    // Primary constructor used by MAUI Dependency Injection
    public TransactionsPage(D1Service d1Service, UserService userService)
    {
        InitializeComponent();
        _d1Service = d1Service;
        _userService = userService;
    }

    // Safe Release fallback constructor
    public TransactionsPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services
                     ?? IPlatformApplication.Current?.Services;

        _d1Service = services?.GetService<D1Service>() ?? new D1Service();
        _userService = services?.GetService<UserService>() ?? new UserService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await LoadSummaryDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TransactionsPage Error]: {ex.Message}");
        }
    }

    private async Task LoadSummaryDataAsync()
    {
        var currentUser = _userService.CurrentUser;
        if (currentUser == null) return;

        const string sql = "SELECT * FROM TransactionRecord WHERE UserId = ? ORDER BY Date DESC;";
        var response = await _d1Service.QueryAsync<Transaction>(sql, currentUser.UserId);

        int totalIncomeCents = 0;
        int totalExpenseCents = 0;
        var freshList = new List<TransactionDisplayModel>();

        if (response?.Success == true && response.Results != null)
        {
            foreach (var tx in response.Results)
            {
                bool isIncome = tx.TransactionType.Equals("Income", StringComparison.OrdinalIgnoreCase);

                if (isIncome) totalIncomeCents += tx.TransactionAmount;
                else totalExpenseCents += tx.TransactionAmount;

                freshList.Add(new TransactionDisplayModel
                {
                    TransactionId = tx.TransactionId,
                    Description = string.IsNullOrWhiteSpace(tx.Description) ? tx.TransactionType : tx.Description,
                    CategoryName = isIncome ? "Income" : "Expense",
                    CategoryIcon = isIncome ? "💵" : GetIconForDescription(tx.Description),
                    DisplayDate = tx.Date.ToString("MMM dd, yyyy"),
                    FormattedAmount = $"{(isIncome ? "+" : "-")}${(tx.TransactionAmount / 100.0):N2}",
                    AmountColor = isIncome ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444")
                });
            }
        }

        _allTransactions = freshList;

        // Force UI updates onto the Main Thread safely for Release builds
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TotalIncomeLabel.Text = $"${(totalIncomeCents / 100.0):N2}";
            TotalExpenseLabel.Text = $"${(totalExpenseCents / 100.0):N2}";
            ApplyFilter();
        });
    }

    private void ApplyFilter()
    {
        if (_currentFilter == "Expenses")
        {
            TransactionsListView.ItemsSource = _allTransactions.Where(t => t.FormattedAmount.StartsWith("-")).ToList();
        }
        else if (_currentFilter == "Income")
        {
            TransactionsListView.ItemsSource = _allTransactions.Where(t => t.FormattedAmount.StartsWith("+")).ToList();
        }
        else
        {
            TransactionsListView.ItemsSource = _allTransactions.ToList();
        }
    }

    private void FilterAll_Clicked(object sender, EventArgs e)
    {
        _currentFilter = "All";
        FilterAllBtn.Style = (Style)Application.Current!.Resources["SegOn"];
        FilterExpenseBtn.Style = (Style)Application.Current!.Resources["SegOff"];
        FilterIncomeBtn.Style = (Style)Application.Current!.Resources["SegOff"];
        ApplyFilter();
    }

    private void FilterExpense_Clicked(object sender, EventArgs e)
    {
        _currentFilter = "Expenses";
        FilterExpenseBtn.Style = (Style)Application.Current!.Resources["SegOn"];
        FilterAllBtn.Style = (Style)Application.Current!.Resources["SegOff"];
        FilterIncomeBtn.Style = (Style)Application.Current!.Resources["SegOff"];
        ApplyFilter();
    }

    private void FilterIncome_Clicked(object sender, EventArgs e)
    {
        _currentFilter = "Income";
        FilterIncomeBtn.Style = (Style)Application.Current!.Resources["SegOn"];
        FilterAllBtn.Style = (Style)Application.Current!.Resources["SegOff"];
        FilterExpenseBtn.Style = (Style)Application.Current!.Resources["SegOff"];
        ApplyFilter();
    }

    private async void QuickAddBtn_Clicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("New Transaction", "Cancel", null, "💸 Add Expense", "💰 Add Income");
        if (action == "💸 Add Expense") await Shell.Current.GoToAsync(nameof(AddExpensePage));
        else if (action == "💰 Add Income") await Shell.Current.GoToAsync(nameof(AddIncomePage));
    }

    private string GetIconForDescription(string desc)
    {
        string d = desc.ToLower();
        if (d.Contains("fresh") || d.Contains("market") || d.Contains("food") || d.Contains("grocery")) return "🛒";
        if (d.Contains("rent") || d.Contains("housing")) return "🏠";
        if (d.Contains("coffee") || d.Contains("cafe")) return "☕";
        if (d.Contains("book") || d.Contains("tuition")) return "📚";
        return "💳";
    }
}