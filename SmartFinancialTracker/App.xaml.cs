using Microsoft.Extensions.DependencyInjection;
using SmartFinancialTracker;
using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Services;

namespace SmartFinancialTracker
{
    public partial class App : Application
    {
        public App(D1Service d1Service)
        {
            InitializeComponent();

            Task.Run(async () =>
            {
                await DatabaseInitializer.EnsureDatabaseCreatedAsync(d1Service);
            });

            // Catch all unhandled exceptions globally across all threads
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"[GLOBAL CRASH]: {ex?.Message}\n{ex?.StackTrace}");
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[TASK CRASH]: {e.Exception.Message}\n{e.Exception.StackTrace}");
                e.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}