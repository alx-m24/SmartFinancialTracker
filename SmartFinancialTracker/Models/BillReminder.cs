using System.Text.Json.Serialization;

namespace SmartFinancialTracker.Models;

using SmartFinancialTracker.Classes;

public class BillReminder : DbEntity
{
    [JsonPropertyName("BillId")]
    public int BillId { get; set; }

    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("BillTitle")]
    public string BillTitle { get; set; } = string.Empty;

    [JsonPropertyName("BillAmount")]
    public int BillAmount { get; set; }

    [JsonPropertyName("DueDate")]
    public string DueDate { get; set; } = string.Empty;

    [JsonPropertyName("IsPaid")]
    public int RawIsPaid { get; set; }

    [JsonIgnore]
    public bool IsPaid
    {
        get => RawIsPaid == 1;
        set => RawIsPaid = value ? 1 : 0;
    }

    [JsonPropertyName("BillIcon")]
    public string BillIcon { get; set; } = "📄";

    public override string CreateSql() => """
    CREATE TABLE IF NOT EXISTS BillReminder (
        BillId INTEGER PRIMARY KEY AUTOINCREMENT,
        UserId TEXT NOT NULL,
        BillTitle TEXT NOT NULL,
        BillAmount INTEGER NOT NULL,
        DueDate TEXT NOT NULL,
        IsPaid INTEGER NOT NULL DEFAULT 0,
        BillIcon TEXT NOT NULL DEFAULT '📄',
        FOREIGN KEY (UserId) REFERENCES User(UserId)
    );
    """;

    [JsonIgnore]
    public string DisplayAmount => $"${BillAmount / 100.0:N2}";

    [JsonIgnore]
    public string DisplayDue
    {
        get
        {
            if (!DateTime.TryParse(DueDate, out var due)) return DueDate;
            int days = (due.Date - DateTime.Today).Days;
            if (days < 0) return $"Overdue by {-days} day(s)";
            return days == 0 ? "Due today" : $"Due in {days} day(s)";
        }
    }
}