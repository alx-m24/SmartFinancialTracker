using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Models;
using System.Globalization;
using System.Text.Json;

namespace SmartFinancialTracker.Tests;

public class SavingsGoalTests
{
    [Fact]
    public void ProgressRatio_HalfSaved_IsPointFive()
    {
        var g = new SavingsGoal { TargetAmount = 50000, CurrentAmount = 25000 };
        Assert.Equal(0.5, g.ProgressRatio, 3);
        Assert.Equal(50, g.PercentInt);
    }

    [Fact]
    public void ProgressRatio_OverTarget_IsCappedAtOne()
    {
        var g = new SavingsGoal { TargetAmount = 1000, CurrentAmount = 5000 };
        Assert.Equal(1.0, g.ProgressRatio);
        Assert.Equal(100, g.PercentInt);
    }

    [Fact]
    public void ProgressRatio_ZeroTarget_IsZero()
    {
        var g = new SavingsGoal { TargetAmount = 0, CurrentAmount = 500 };
        Assert.Equal(0, g.ProgressRatio);
    }

    [Fact]
    public void DisplayProgress_ConvertsCentsToDollars()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        var g = new SavingsGoal { TargetAmount = 50000, CurrentAmount = 35000 };
        Assert.Equal("$350 / $500 saved", g.DisplayProgress);
    }
}

public class BillReminderTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void IsPaid_MapsFromRawInt(int raw, bool expected)
    {
        Assert.Equal(expected, new BillReminder { RawIsPaid = raw }.IsPaid);
    }

    [Fact]
    public void IsPaid_Setter_UpdatesRawValue()
    {
        var b = new BillReminder { IsPaid = true };
        Assert.Equal(1, b.RawIsPaid);
    }

    [Fact]
    public void Json_IsPaidOne_DeserializesToTrue()
    {
        var b = JsonSerializer.Deserialize<BillReminder>("""{"BillId":1,"IsPaid":1}""");
        Assert.True(b!.IsPaid);
    }

    [Fact]
    public void DisplayAmount_ConvertsCentsToDollars()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Assert.Equal("$250.00", new BillReminder { BillAmount = 25000 }.DisplayAmount);
    }

    [Theory]
    [InlineData(3, "Due in 3 day(s)")]
    [InlineData(0, "Due today")]
    [InlineData(-2, "Overdue by 2 day(s)")]
    public void DisplayDue_ReflectsDaysFromToday(int offsetDays, string expected)
    {
        var b = new BillReminder { DueDate = DateTime.Today.AddDays(offsetDays).ToString("yyyy-MM-dd") };
        Assert.Equal(expected, b.DisplayDue);
    }

    [Fact]
    public void DisplayDue_InvalidDate_ReturnsRawText()
    {
        Assert.Equal("soon", new BillReminder { DueDate = "soon" }.DisplayDue);
    }
}

public class EntityTests
{
    [Fact]
    public void User_TwoFactor_MapsAndVerifies()
    {
        var u = new User { RawTwoFactorAuth = 1 };
        Assert.True(u.TwoFactorAuth);
        Assert.True(u.IdentityVerification());
        Assert.False(new User().IdentityVerification());
    }

    [Fact]
    public void Transaction_DetectFraud_SetterUpdatesRaw()
    {
        var t = new Transaction { DetectFraud = true };
        Assert.Equal(1, t.RawDetectFraud);
    }

    [Theory]
    [InlineData(500, 1000, true)]
    [InlineData(1000, 1000, true)]
    [InlineData(1001, 1000, false)]
    public void MonthlyBudget_CheckStatus(int spent, int limit, bool expected)
    {
        var b = new MonthlyBudget { CurrentSpending = spent };
        b.SetLimit(limit);
        Assert.Equal(expected, b.CheckStatus());
    }

    [Fact]
    public void BankAccount_CheckBalance_ReturnsBalance()
    {
        Assert.Equal(100000, new BankAccount { BankBalance = 100000 }.CheckBalance());
    }
}