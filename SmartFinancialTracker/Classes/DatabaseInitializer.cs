using SmartFinancialTracker.Services;
using SmartFinancialTracker.Models;

namespace SmartFinancialTracker.Classes;

public static class DatabaseInitializer
{
    // Define all entities representing required tables
    private static readonly DbEntity[] RequiredEntities =
    [
        new User(),
        new BankAccount(),
        new Category(),
        new MonthlyBudget(),
        new Transaction(),
        new SavingsGoal(),
        new BillReminder()
    ];

    // Map model names to actual SQL table names created by CreateSql()
    private static readonly HashSet<string> ExpectedTableNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "User",
        "BankAccount",
        "Category",
        "MonthlyBudget",
        "TransactionRecord",
        "SavingsGoal",
        "BillReminder"
    };

    public class SqliteSchemaRow
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Checks database tables on startup. If any table is missing, drops all tables and rebuilds the schema.
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(D1Service d1)
    {
        await RecreateIfStaleAsync(d1, "SavingsGoal", "TargetAmount");
        await RecreateIfStaleAsync(d1, "BillReminder", "BillTitle");

        foreach (var entity in RequiredEntities)
            await d1.QueryAsync<object>(entity.CreateSql());   // idempotent
    }

    private static async Task RecreateIfStaleAsync(D1Service d1, string table, string expectedColumn)
    {
        var r = await d1.QueryAsync<SqliteSchemaRow>($"SELECT name FROM pragma_table_info('{table}');");
        var cols = r?.Results?.Select(c => c.Name).ToList() ?? new();
        if (cols.Count > 0 && !cols.Contains(expectedColumn, StringComparer.OrdinalIgnoreCase))
            await d1.QueryAsync<object>($"DROP TABLE {table};");
    }

    private static async Task ResetDatabaseAsync(D1Service d1Service, IEnumerable<string> existingTables)
    {
        // Disable Foreign Keys temporarily to avoid drop order constraints
        await d1Service.QueryAsync<object>("PRAGMA foreign_keys = OFF;");

        // Drop any existing tables to ensure a clean slate
        foreach (var tableName in existingTables)
        {
            await d1Service.QueryAsync<object>($"DROP TABLE IF EXISTS {tableName};");
        }

        // Re-enable Foreign Keys
        await d1Service.QueryAsync<object>("PRAGMA foreign_keys = ON;");

        // Create all tables using CreateSql()
        foreach (var entity in RequiredEntities)
        {
            await d1Service.QueryAsync<object>(entity.CreateSql());
        }
    }
}