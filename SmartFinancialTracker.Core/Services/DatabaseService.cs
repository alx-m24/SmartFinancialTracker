using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartFinancialTracker.Services;

#region Request & Response Models

public record D1Request(
    [property: JsonPropertyName("sql")] string Sql,
    [property: JsonPropertyName("params")] object[] Params
);

public class D1Response<T>
{
    [JsonPropertyName("results")]
    public List<T>? Results { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("meta")]
    public D1Meta? Meta { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class D1Meta
{
    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("changes")]
    public int Changes { get; set; }

    [JsonPropertyName("last_row_id")]
    public int LastRowId { get; set; }
}

#endregion

public class D1Service
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://smartfinancialtracker.alexandremohamudally.workers.dev/api/d1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public D1Service(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<D1Response<T>?> QueryAsync<T>(string sql, params object[] parameters)
    {
        var payload = new D1Request(sql, parameters);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, payload);
            string jsonString = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"[D1 Raw Response]: {jsonString}");

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new D1Response<T>
                {
                    Success = false,
                    Error = "Empty Response",
                    Message = "Worker returned an empty response."
                };
            }

            // Deserialize with fallback safety
            var result = JsonSerializer.Deserialize<D1Response<T>>(jsonString, JsonOptions);

            // If D1 returned an explicit error JSON like {"error": "...", "message": "..."}
            if (result != null && !string.IsNullOrEmpty(result.Error))
            {
                result.Success = false;
            }

            return result;
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"[D1 JSON Deserialization Error]: {jsonEx.Message}");
            return new D1Response<T>
            {
                Success = false,
                Error = "JSON Parsing Error",
                Message = jsonEx.Message
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[D1 Client Error]: {ex.Message}");
            return new D1Response<T>
            {
                Success = false,
                Error = "Client Error",
                Message = ex.Message
            };
        }
    }
}