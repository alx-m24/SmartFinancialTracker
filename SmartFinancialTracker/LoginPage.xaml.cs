namespace SmartFinancialTracker;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

    private async void SignInBtn_Clicked(object sender, EventArgs e)
    {
		await Shell.Current.GoToAsync("//Main");
    }
}