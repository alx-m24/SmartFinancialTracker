namespace SmartFinancialTracker.Models;

public class TransactionDisplayModel
{
    public string TransactionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = "General";
    public string CategoryIcon { get; set; } = "🛒";
    public string DisplayDate { get; set; } = string.Empty;
    public string FormattedAmount { get; set; } = string.Empty;
    public string AmountColorHex { get; set; } = "#000000";
}