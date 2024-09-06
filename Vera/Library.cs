using System.Text;
using System.Text.Json;

namespace Vera
{
    /// <summary>
    /// Represents a client for interacting with the Gemini API.
    /// </summary>
    /// <param name="apiKey">The API key for authenticating with the Gemini API.</param>
    /// <param name="model">The Gemini model to use. Defaults to <see cref="GeminiModel.Gemini15FlashLatest"/>.</param>
    /// <param name="httpClient">An optional <see cref="HttpClient"/> instance. If not provided, a new one will be created.</param>
    public class Gemini(
        string apiKey,
        Gemini.GeminiModel model = Gemini.GeminiModel.Gemini15FlashLatest,
        HttpClient? httpClient = null
    )
    {
        private const string ApiBaseUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/";
        private readonly string modelName = GetModelName(model);
        private readonly string apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        private readonly HttpClient httpClient = httpClient ?? new HttpClient();

        /// <summary>
        /// Represents the available Gemini models.
        /// </summary>
        public enum GeminiModel
        {
            /// <summary>Gemini Pro Vision model</summary>
            GeminiProVision,

            /// <summary>Gemini Pro model</summary>
            GeminiPro,

            /// <summary>Gemini 1.0 Pro model</summary>
            Gemini10Pro,

            /// <summary>Gemini 1.5 Flash model</summary>
            Gemini15Flash,

            /// <summary>Gemini 1.5 Pro model</summary>
            Gemini15Pro,

            /// <summary>Latest Gemini 1.5 Flash model</summary>
            Gemini15FlashLatest,

            /// <summary>Latest Gemini 1.5 Pro model</summary>
            Gemini15ProLatest,
        }

        /// <summary>
        /// Gets the API model name corresponding to the specified <see cref="GeminiModel"/> enum value.
        /// </summary>
        /// <param name="model">The <see cref="GeminiModel"/> enum value.</param>
        /// <returns>The corresponding API model name.</returns>
        /// <exception cref="ArgumentException">Thrown when an invalid Gemini model is specified.</exception>
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

        /// <summary>
        /// Sends a prompt to the Gemini API and retrieves the generated response.
        /// </summary>
        /// <param name="prompt">The input prompt to send to the Gemini API.</param>
        /// <returns>The generated response from the Gemini API.</returns>
        /// <exception cref="ArgumentException">Thrown when the prompt is null or empty.</exception>
        /// <exception cref="GeminiApiException">Thrown when there's an error communicating with the Gemini API or parsing the response.</exception>
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

        /// <summary>
        /// Extracts the generated text from the Gemini API response.
        /// </summary>
        /// <param name="responseBody">The JSON response body from the Gemini API.</param>
        /// <returns>The extracted text from the response.</returns>
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

    /// <summary>
    /// Represents an exception that occurs during interactions with the Gemini API.
    /// </summary>
    public class GeminiApiException(string message, Exception innerException)
        : Exception(message, innerException) { }
};
