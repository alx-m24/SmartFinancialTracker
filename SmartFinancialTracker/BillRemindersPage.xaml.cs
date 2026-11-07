using SmartFinancialTracker.Models;
using SmartFinancialTracker.Services;
using SmartFinancialTracker.Classes;

namespace SmartFinancialTracker;

public partial class BillRemindersPage : ContentPage
{
    private readonly D1Service _d1Service;
    private readonly UserService _userService;

    public BillRemindersPage(D1Service d1Service, UserService userService)
    {
        InitializeComponent();
        _d1Service = d1Service;
        _userService = userService;
    }

    public BillRemindersPage() : this(
        IPlatformApplication.Current?.Services.GetService<D1Service>() ?? new D1Service(),
        IPlatformApplication.Current?.Services.GetService<UserService>() ?? new UserService())
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadBillDataAsync();
    }

    private async Task LoadBillDataAsync()
    {
        var currentUser = _userService.CurrentUser;
        if (currentUser == null) return;

        const string billsSql = "SELECT * FROM BillReminder WHERE UserId = ? ORDER BY DueDate;";
        var response = await _d1Service.QueryAsync<BillReminder>(billsSql, currentUser.UserId);
        if (response?.Success != true)
            System.Diagnostics.Debug.WriteLine($"[Bills] {response?.Message}");

        var all = response?.Results ?? new List<BillReminder>();

        if (all.Count == 0)   // only seed when the user has no bills at all
        {
            await SeedDefaultBillsAsync(currentUser.UserId);
            response = await _d1Service.QueryAsync<BillReminder>(billsSql, currentUser.UserId);
            all = response?.Results ?? new List<BillReminder>();
        }

        var unpaid = all.Where(b => !b.IsPaid).ToList();
        BillsListView.ItemsSource = unpaid;
        UnpaidCountLabel.Text = unpaid.Count.ToString();

        const string txSql = "SELECT * FROM TransactionRecord WHERE UserId = ?;";
        var txResponse = await _d1Service.QueryAsync<Transaction>(txSql, currentUser.UserId);
        int totalExpensesCents = txResponse?.Results?
            .Where(t => t.TransactionType.Equals("Expense", StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.TransactionAmount) ?? 0;
        TotalSpentLabel.Text = $"${totalExpensesCents / 100.0:N2}";
    }

    private async Task SeedDefaultBillsAsync(string userId)
    {
        string due1 = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");
        string due2 = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");

        const string insertSql = """
        INSERT INTO BillReminder (UserId, BillTitle, BillAmount, DueDate, IsPaid, BillIcon)
        VALUES (?, 'Tuition Fee Installment', 25000, ?, 0, '🎓'),
               (?, 'Mobile Data Plan', 3500, ?, 0, '📱');
        """;
        await _d1Service.QueryAsync<object>(insertSql, userId, due1, userId, due2);
    }

    private async void PayBillBtn_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is BillReminder bill)
        {
            bool confirm = await DisplayAlertAsync("Pay Bill", $"Mark '{bill.BillTitle}' as paid?", "Yes", "No");
            if (confirm)
            {
                const string updateSql = "UPDATE BillReminder SET IsPaid = 1 WHERE BillId = ?;";
                await _d1Service.QueryAsync<object>(updateSql, bill.BillId);
                await LoadBillDataAsync();
            }
        }
    }
}