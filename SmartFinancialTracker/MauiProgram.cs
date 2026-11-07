using Microsoft.Extensions.Logging;
using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Services;

namespace SmartFinancialTracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Register Services for Dependency Injection
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<D1Service>();
            builder.Services.AddSingleton<UserService>();

            // Register Pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<TransactionsPage>();
            builder.Services.AddTransient<SavingGoalsPage>();
            builder.Services.AddTransient<BillRemindersPage>();
            builder.Services.AddTransient<AddExpensePage>();
            builder.Services.AddTransient<AddIncomePage>();

            // Native Entry mapping (Remove background underlines across platforms)
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.Background = null;
#elif IOS || MACCATALYST
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
            });

            return builder.Build();
        }
    }
}