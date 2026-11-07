using SmartFinancialTracker.Models;
using SmartFinancialTracker.Services;

namespace SmartFinancialTracker;

public partial class SavingGoalsPage : ContentPage
{
    private readonly D1Service _d1Service;
    private readonly UserService _userService;

    public SavingGoalsPage(D1Service d1Service, UserService userService)
    {
        InitializeComponent();
        _d1Service = d1Service;
        _userService = userService;
    }

    public SavingGoalsPage() : this(
        IPlatformApplication.Current?.Services.GetService<D1Service>() ?? new D1Service(),
        IPlatformApplication.Current?.Services.GetService<UserService>() ?? new UserService())
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSavingsGoalsAsync();
    }

    private async Task LoadSavingsGoalsAsync()
    {
        var currentUser = _userService.CurrentUser;
        if (currentUser == null) return;

        const string sql = "SELECT * FROM SavingsGoal WHERE UserId = ?;";
        var response = await _d1Service.QueryAsync<SavingsGoal>(sql, currentUser.UserId);

        var goals = response?.Results ?? new List<SavingsGoal>();

        // Auto-seed default goals if none exist for new users
        if (goals.Count == 0)
        {
            await SeedDefaultGoalsAsync(currentUser.UserId);
            var reFetch = await _d1Service.QueryAsync<SavingsGoal>(sql, currentUser.UserId);
            goals = reFetch?.Results ?? new List<SavingsGoal>();
        }

        int totalSavedCents = goals.Sum(g => g.CurrentAmount);
        GoalsSummaryLabel.Text = $"{goals.Count} goals · ${(totalSavedCents / 100.0):N0} saved";

        GoalsListView.ItemsSource = goals;
    }

    private async Task SeedDefaultGoalsAsync(string userId)
    {
        const string insertSql = """
            INSERT INTO SavingsGoal (UserId, GoalName, TargetAmount, CurrentAmount, GoalIcon)
            VALUES (?, 'Emergency Fund', 50000, 35000, '🛟'),
                   (?, 'Summer Vacation', 40000, 12000, '🏖️');
            """;
        await _d1Service.QueryAsync<object>(insertSql, userId, userId);
    }

    private async void CreateGoalBtn_Clicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("New Goal", "Enter goal name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        string targetStr = await DisplayPromptAsync("Target Amount", "Enter target amount ($):", keyboard: Keyboard.Numeric);
        if (double.TryParse(targetStr, out double targetDollars) && targetDollars > 0)
        {
            int targetCents = (int)Math.Round(targetDollars * 100);
            const string insertSql = """
                INSERT INTO SavingsGoal (UserId, GoalName, TargetAmount, CurrentAmount, GoalIcon)
                VALUES (?, ?, ?, 0, '🎯');
                """;
            var result = await _d1Service.QueryAsync<object>(insertSql, _userService.CurrentUser?.UserId, name, targetCents);
            if (result?.Success != true)
                await DisplayAlertAsync("Error", "Could not save goal.", "OK");
            await LoadSavingsGoalsAsync();
        }
    }
}