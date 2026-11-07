using SmartFinancialTracker.Classes;

namespace SmartFinancialTracker.Services;

public class UserService
{
    /// <summary>
    /// Holds the currently logged-in user details across the app.
    /// </summary>
    public User? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null;

    public void Login(User user)
    {
        CurrentUser = user;
    }

    public void Logout()
    {
        CurrentUser = null;
    }
}