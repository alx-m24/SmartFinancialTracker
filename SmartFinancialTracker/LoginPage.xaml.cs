using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Services;

namespace SmartFinancialTracker;

public partial class LoginPage : ContentPage
{
    private readonly D1Service _d1Service;
    private readonly UserService _userService;

    public LoginPage(D1Service d1Service, UserService userService)
    {
        InitializeComponent();
        _d1Service = d1Service;
        _userService = userService;
    }

    private async void SignInBtn_Clicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Error", "Please enter both email and password.", "OK");
            return;
        }

        SignInBtn.IsEnabled = false;

        try
        {
            // Query Cloudflare D1 for matching user credentials
            const string sql = "SELECT * FROM User WHERE Email = ? AND Password = ? LIMIT 1;";
            var response = await _d1Service.QueryAsync<User>(sql, email, password);

            // 🔑 Check if response is successful AND contains at least 1 matching user row
            if (response?.Success == true && response.Results != null && response.Results.Count > 0)
            {
                User loggedInUser = response.Results.FirstOrDefault()!;

                // Check 2FA if enabled
                if (loggedInUser.IdentityVerification())
                {
                    // Optionally route to 2FA verification page here
                }

                // Save user to global service
                _userService.Login(loggedInUser);

                // Navigate to Main application shell
                await Shell.Current.GoToAsync("//Main");
            }
            else
            {
                await DisplayAlertAsync("Login Failed", "Invalid email or password.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
        finally
        {
            SignInBtn.IsEnabled = true;
        }
    }

    private async void SignupBtn_Clicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Error", "Please enter an email and password to sign up.", "OK");
            return;
        }

        try
        {
            // Check if user already exists
            const string checkSql = "SELECT * FROM User WHERE Email = ? LIMIT 1;";
            var checkResponse = await _d1Service.QueryAsync<User>(checkSql, email);

            if (checkResponse?.Results != null && checkResponse.Results.Count > 0)
            {
                await DisplayAlertAsync("Signup Failed", "An account with this email already exists.", "OK");
                return;
            }

            // Create new user record
            var newUser = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = email.Split('@')[0],
                Email = email,
                Password = password,
                TwoFactorAuth = false
            };

            const string insertSql = """
                INSERT INTO User (UserId, Username, Password, Email, TwoFactorAuth)
                VALUES (?, ?, ?, ?, ?);
                """;

            var insertResponse = await _d1Service.QueryAsync<object>(
                insertSql,
                newUser.UserId,
                newUser.Username,
                newUser.Password,
                newUser.Email,
                newUser.TwoFactorAuth ? 1 : 0
            );

            if (insertResponse?.Success == true)
            {
                await DisplayAlertAsync("Success", "Account created successfully! Logging you in...", "OK");
                _userService.Login(newUser);

                const string checkAccountSql = "SELECT * FROM BankAccount WHERE UserId = ? LIMIT 1;";
                var bankCheck = await _d1Service.QueryAsync<BankAccount>(checkAccountSql, newUser.UserId);

                if (bankCheck?.Results == null || bankCheck.Results.Count == 0)
                {
                    // Create initial bank account with standard starting balance ($1,000.00 / 100000 cents)
                    await CreateInitialBankAccountAsync(newUser.UserId);
                }

                await Shell.Current.GoToAsync("//Main");
            }
            else
            {
                await DisplayAlertAsync("Error", "Failed to create account. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Signup failed: {ex.Message}", "OK");
        }
    }

    private async Task CreateInitialBankAccountAsync(string userId)
    {
        const string insertAccountSql = """
        INSERT INTO BankAccount (UserId, BankName, AccountType, BankBalance)
        VALUES (?, 'Primary Checking', 'Checking', 100000);
        """;

        var result = await _d1Service.QueryAsync<object>(insertAccountSql, userId);

        if (result == null || !result.Success)
        {
            System.Diagnostics.Debug.WriteLine($"[Bank Account Creation Failed]: {result?.Message}");
        }
    }
}