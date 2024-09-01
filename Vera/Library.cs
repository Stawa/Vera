namespace Vera;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class Gemini
{
    private const string ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
    private readonly string modelName;
    private readonly string apiKey;
    private readonly HttpClient httpClient;

    public enum GeminiModel
    {
        GeminiProVision,
        GeminiPro,
        Gemini10Pro,
        Gemini15Flash,
        Gemini15Pro,
        Gemini15FlashLatest,
        Gemini15ProLatest,
    }

    public Gemini(
        string apiKey,
        GeminiModel model = GeminiModel.Gemini15FlashLatest,
        HttpClient? httpClient = null
    )
    {
        this.apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        this.httpClient = httpClient ?? new HttpClient();
        modelName = GetModelName(model);
    }

    private static string GetModelName(GeminiModel model) =>
        model switch
        {
            GeminiModel.GeminiProVision => "gemini-pro-vision",
            GeminiModel.GeminiPro => "gemini-pro",
            GeminiModel.Gemini10Pro => "gemini-1.0-pro",
            GeminiModel.Gemini15Flash => "gemini-1.5-flash",
            GeminiModel.Gemini15Pro => "gemini-1.5-pro",
            GeminiModel.Gemini15FlashLatest => "gemini-1.5-flash-latest",
            GeminiModel.Gemini15ProLatest => "gemini-1.5-pro-latest",
            _ => throw new ArgumentException("Invalid Gemini model specified", nameof(model)),
        };

    public async Task<string> FetchResponseAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
        };
        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await httpClient.PostAsync(
                $"{ApiBaseUrl}{modelName}:generateContent?key={apiKey}",
                content
            );
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return ExtractTextFromResponse(responseBody);
        }
        catch (HttpRequestException ex)
        {
            throw new GeminiApiException("Error communicating with Gemini API", ex);
        }
        catch (JsonException ex)
        {
            throw new GeminiApiException("Error parsing Gemini API response", ex);
        }
    }

    private static string ExtractTextFromResponse(string responseBody)
    {
        var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
        return result
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
    }
}

public class GeminiApiException(string message, Exception innerException)
    : Exception(message, innerException) { }
