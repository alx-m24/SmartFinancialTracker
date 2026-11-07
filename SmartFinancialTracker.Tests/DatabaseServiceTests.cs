using SmartFinancialTracker.Classes;
using SmartFinancialTracker.Models;
using SmartFinancialTracker.Services;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SmartFinancialTracker.Tests;

public class D1CrudTests
{
    private D1Service CreateMockD1Service(string jsonResponseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new 
            MockHttpMessageHandler(jsonResponseBody, statusCode);
        var httpClient = new HttpClient(handler);
        return new D1Service(httpClient);
    }

    [Fact]
    public async Task User_CrudFlow_CreateReadUpdateDelete()
    {
        // --- 1. CREATE USER ---
        string createResponse = WrapD1Result("""[{"GoalId":1}]"""); // D1 returns meta/success for INSERT
        var d1Create = CreateMockD1Service(createResponse);

        const string insertSql = "INSERT INTO User (UserId, UserName, Email, RawTwoFactorAuth) VALUES (?, ?, ?, ?);";
        var createResult = await d1Create.QueryAsync<object>(insertSql, "usr_100", "Alice Smith", "alice@test.com", 1);

        Assert.NotNull(createResult);
        Assert.True(createResult.Success);

        // --- 2. READ (GET) USER ---
        string readResponse = WrapD1Result("""
        [
            {
                "UserId": "usr_100",
                "UserName": "Alice Smith",
                "Email": "alice@test.com",
                "RawTwoFactorAuth": 1
            }
        ]
        """);
        var d1Read = CreateMockD1Service(readResponse);

        const string selectSql = "SELECT * FROM User WHERE UserId = ?;";
        var readResult = await d1Read.QueryAsync<User>(selectSql, "usr_100");

        Assert.NotNull(readResult);
        Assert.True(readResult.Success);
        Assert.Single(readResult.Results);

        var user = readResult.Results[0];
        Assert.Equal("usr_100", user.UserId);
        Assert.Equal("Alice Smith", user.Username);
        // Assert.True(user.TwoFactorAuth);
        // Assert.True(user.IdentityVerification());

        // --- 3. UPDATE USER ---
        string updateResponse = WrapD1Result("[]");
        var d1Update = CreateMockD1Service(updateResponse);

        const string updateSql = "UPDATE User SET UserName = ? WHERE UserId = ?;";
        var updateResult = await d1Update.QueryAsync<object>(updateSql, "Alice Johnson", "usr_100");

        Assert.NotNull(updateResult);
        Assert.True(updateResult.Success);

        // --- 4. DELETE USER ---
        string deleteResponse = WrapD1Result("[]");
        var d1Delete = CreateMockD1Service(deleteResponse);

        const string deleteSql = "DELETE FROM User WHERE UserId = ?;";
        var deleteResult = await d1Delete.QueryAsync<object>(deleteSql, "usr_100");

        Assert.NotNull(deleteResult);
        Assert.True(deleteResult.Success);
    }

    [Fact]
    public async Task SavingsGoal_CrudFlow_CreateReadUpdateDelete()
    {
        // --- CREATE GOAL ---
        var d1Create = CreateMockD1Service(WrapD1Result("[]"));
        const string insertGoal = "INSERT INTO SavingsGoal (UserId, GoalName, TargetAmount, CurrentAmount, GoalIcon) VALUES (?, ?, ?, ?, ?);";
        var createRes = await d1Create.QueryAsync<object>(insertGoal, "usr_100", "New Laptop", 150000, 50000, "💻");
        Assert.True(createRes.Success);

        // --- READ GOAL ---
        string readGoalJson = WrapD1Result("""
        [
            {
                "GoalId": 10,
                "UserId": "usr_100",
                "GoalName": "New Laptop",
                "TargetAmount": 150000,
                "CurrentAmount": 50000,
                "GoalIcon": "💻"
            }
        ]
        """);
        var d1Read = CreateMockD1Service(readGoalJson);
        var readRes = await d1Read.QueryAsync<SavingsGoal>("SELECT * FROM SavingsGoal WHERE GoalId = ?;", 10);

        Assert.Single(readRes.Results);
        var goal = readRes.Results[0];
        Assert.Equal(150000, goal.TargetAmount);
        Assert.Equal(50000, goal.CurrentAmount);
        Assert.Equal(0.333, goal.ProgressRatio, 3);
        Assert.Equal(33, goal.PercentInt);

        // --- UPDATE GOAL (Add Deposit) ---
        var d1Update = CreateMockD1Service(WrapD1Result("[]"));
        var updateRes = await d1Update.QueryAsync<object>("UPDATE SavingsGoal SET CurrentAmount = ? WHERE GoalId = ?;", 100000, 10);
        Assert.True(updateRes.Success);

        // --- DELETE GOAL ---
        var d1Delete = CreateMockD1Service(WrapD1Result("[]"));
        var deleteRes = await d1Delete.QueryAsync<object>("DELETE FROM SavingsGoal WHERE GoalId = ?;", 10);
        Assert.True(deleteRes.Success);
    }

    [Fact]
    public async Task BillReminder_CrudFlow_PayBillAndUpdateStatus()
    {
        // --- READ UNPAID BILL ---
        string billJson = WrapD1Result("""
        [
            {
                "BillId": 5,
                "UserId": "usr_100",
                "BillTitle": "Tuition Fee",
                "BillAmount": 25000,
                "DueDate": "2026-11-15",
                "IsPaid": 0,
                "BillIcon": "🎓"
            }
        ]
        """);
        var d1Read = CreateMockD1Service(billJson);
        var readRes = await d1Read.QueryAsync<BillReminder>("SELECT * FROM BillReminder WHERE BillId = ?;", 5);

        var bill = readRes.Results[0];
        Assert.False(bill.IsPaid);
        Assert.Equal("$250.00", bill.DisplayAmount);

        // --- MARK AS PAID (UPDATE) ---
        bill.IsPaid = true; // Setter updates RawIsPaid to 1
        Assert.Equal(1, bill.RawIsPaid);

        var d1Update = CreateMockD1Service(WrapD1Result("[]"));
        var updateRes = await d1Update.QueryAsync<object>("UPDATE BillReminder SET IsPaid = ? WHERE BillId = ?;", bill.RawIsPaid, bill.BillId);
        Assert.True(updateRes.Success);

        // --- DELETE PAID BILL ---
        var d1Delete = CreateMockD1Service(WrapD1Result("[]"));
        var deleteRes = await d1Delete.QueryAsync<object>("DELETE FROM BillReminder WHERE BillId = ?;", bill.BillId);
        Assert.True(deleteRes.Success);
    }

    // Helper method to wrap raw SQL results into Cloudflare D1 HTTP response format
    private static string WrapD1Result(string resultsArrayJson)
    {
        return $$"""
    {
        "results": {{resultsArrayJson}},
        "success": true,
        "errors": [],
        "messages": []
    }
    """;
    }
}

// Helper HTTP Handler to mock Cloudflare D1 HTTP API calls in unit tests
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}