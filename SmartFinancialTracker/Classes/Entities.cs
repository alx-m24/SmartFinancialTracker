using System.Text.Json.Serialization;

namespace SmartFinancialTracker.Classes;

/// <summary>
/// Abstract base class for database entities.
/// </summary>
public abstract class DbEntity
{
    /// <summary>
    /// Generates the SQL statement to create the database table schema for the entity.
    /// </summary>
    public abstract string CreateSql();
}

public class User : DbEntity
{
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("Username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("Password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("Email")]
    public string Email { get; set; } = string.Empty;

    // Map the integer 0 or 1 directly from D1 JSON
    [JsonPropertyName("TwoFactorAuth")]
    public int RawTwoFactorAuth { get; set; }

    // Helper property to work with standard bools across your app
    [JsonIgnore]
    public bool TwoFactorAuth
    {
        get => RawTwoFactorAuth == 1;
        set => RawTwoFactorAuth = value ? 1 : 0;
    }

    public override string CreateSql()
    {
        return """
            CREATE TABLE IF NOT EXISTS User (
                UserId TEXT PRIMARY KEY,
                Username TEXT NOT NULL,
                Password TEXT NOT NULL,
                Email TEXT NOT NULL,
                TwoFactorAuth INTEGER NOT NULL DEFAULT 0
            );
            """;
    }

    public bool IdentityVerification()
    {
        return TwoFactorAuth;
    }
}

public class BankAccount : DbEntity
{
    public int AccountId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public int BankBalance { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public int TransactionLimit { get; set; }
    public string UserId { get; set; } = string.Empty;

    public override string CreateSql()
    {
        return """
            CREATE TABLE IF NOT EXISTS BankAccount (
                AccountId INTEGER PRIMARY KEY AUTOINCREMENT,
                BankName TEXT NOT NULL,
                BankBalance INTEGER NOT NULL DEFAULT 0,
                AccountType TEXT NOT NULL,
                TransactionLimit INTEGER NOT NULL DEFAULT 0,
                UserId TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES User(UserId)
            );
            """;
    }

    public void ImportData() { }
    public List<object> GetTransactions() => new();
    public int CheckBalance() => BankBalance;
}

public class Category : DbEntity
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public override string CreateSql()
    {
        return """
            CREATE TABLE IF NOT EXISTS Category (
                CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryName TEXT NOT NULL
            );
            """;
    }
}

public class MonthlyBudget : DbEntity
{
    public int BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public int BudgetAmount { get; set; }
    public int CurrentSpending { get; set; }
    public string Month { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool BudgetStatus { get; set; }
    public int CategoryId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public override string CreateSql()
    {
        return """
            CREATE TABLE IF NOT EXISTS MonthlyBudget (
                BudgetId INTEGER PRIMARY KEY AUTOINCREMENT,
                BudgetName TEXT NOT NULL,
                BudgetAmount INTEGER NOT NULL,
                CurrentSpending INTEGER NOT NULL DEFAULT 0,
                Month TEXT NOT NULL,
                Type TEXT NOT NULL,
                BudgetStatus INTEGER NOT NULL DEFAULT 1,
                CategoryId INTEGER NOT NULL,
                UserId TEXT NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Category(CategoryId),
                FOREIGN KEY (UserId) REFERENCES User(UserId)
            );
            """;
    }

    public void SetLimit(int amount) => BudgetAmount = amount;
    public bool CheckStatus() => CurrentSpending <= BudgetAmount;
}
public class Transaction : DbEntity
{
    [JsonPropertyName("TransactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("TransactionAmount")]
    public int TransactionAmount { get; set; }

    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("TransactionType")]
    public string TransactionType { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;

    // Map the integer 0 or 1 directly from SQLite/D1
    [JsonPropertyName("DetectFraud")]
    public int RawDetectFraud { get; set; }

    // Helper property to work with standard bools across your app
    [JsonIgnore]
    public bool DetectFraud
    {
        get => RawDetectFraud == 1;
        set => RawDetectFraud = value ? 1 : 0;
    }

    [JsonPropertyName("ReceiptImg")]
    public string ReceiptImg { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("CategoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("AccountId")]
    public int AccountId { get; set; }

    public override string CreateSql()
    {
        return """
            CREATE TABLE IF NOT EXISTS TransactionRecord (
                TransactionId TEXT PRIMARY KEY,
                TransactionAmount INTEGER NOT NULL,
                Date TEXT NOT NULL,
                TransactionType TEXT NOT NULL,
                Description TEXT,
                DetectFraud INTEGER NOT NULL DEFAULT 0,
                ReceiptImg TEXT,
                Status TEXT NOT NULL,
                UserId TEXT NOT NULL,
                CategoryId INTEGER NOT NULL,
                AccountId INTEGER NOT NULL,
                FOREIGN KEY (UserId) REFERENCES User(UserId),
                FOREIGN KEY (CategoryId) REFERENCES Category(CategoryId),
                FOREIGN KEY (AccountId) REFERENCES BankAccount(AccountId)
            );
            """;
    }

    public void Record() { }
    public List<Transaction> Search(string query) => new();
    public void Edit() { }
    public void Delete() { }
}