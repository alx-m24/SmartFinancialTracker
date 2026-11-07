using System.Text.Json.Serialization;

namespace SmartFinancialTracker.Models;

using SmartFinancialTracker.Classes;

public class SavingsGoal : DbEntity
{
    [JsonPropertyName("GoalId")]
    public int GoalId { get; set; }

    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("GoalName")]
    public string GoalName { get; set; } = string.Empty;

    [JsonPropertyName("TargetAmount")]
    public int TargetAmount { get; set; }

    [JsonPropertyName("CurrentAmount")]
    public int CurrentAmount { get; set; }

    [JsonPropertyName("GoalIcon")]
    public string GoalIcon { get; set; } = "🎯";

    // Helper UI properties
    [JsonIgnore]
    public double ProgressRatio => TargetAmount > 0 ? Math.Min((double)CurrentAmount / TargetAmount, 1.0) : 0;

    [JsonIgnore]
    public int PercentInt => (int)(ProgressRatio * 100);

    [JsonIgnore]
    public string DisplayProgress => $"${(CurrentAmount / 100.0):N0} / ${(TargetAmount / 100.0):N0} saved";

    public override string CreateSql() => """
    CREATE TABLE IF NOT EXISTS SavingsGoal (
        GoalId INTEGER PRIMARY KEY AUTOINCREMENT,
        UserId TEXT NOT NULL,
        GoalName TEXT NOT NULL,
        TargetAmount INTEGER NOT NULL,
        CurrentAmount INTEGER NOT NULL DEFAULT 0,
        GoalIcon TEXT NOT NULL DEFAULT '🎯',
        FOREIGN KEY (UserId) REFERENCES User(UserId)
    );
    """;
}