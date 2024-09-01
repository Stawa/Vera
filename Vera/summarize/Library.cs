using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vera.Summarize
{
    public class SummarizeText
    {
        private readonly Dictionary<string, string> apiUrls;
        private readonly Dictionary<string, string> apiTokens;
        private readonly HttpClient httpClient;
        private readonly bool logger;

        public SummarizeText(
            Dictionary<string, string> apiTokens,
            HttpClient httpClient,
            bool logger = false
        )
        {
            this.apiTokens = apiTokens;
            this.httpClient = httpClient;
            this.logger = logger;
            apiUrls = new Dictionary<string, string>
            {
                { "Deepgram", "https://api.deepgram.com/v1/read" },
                { "Edenai", "https://api.edenai.run/v2/text/summarize" },
            };
        }

        public async Task<string> DeepgramAsync(string text, string languageCode)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, apiUrls["Deepgram"]);
                request.Headers.Add("Authorization", $"Token {apiTokens["Deepgram"]}");

                var jsonBody = JsonSerializer.Serialize(new { text = text });

                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var queryParams = new Dictionary<string, string>
                {
                    { "summarize", "v2" },
                    { "topics", "true" },
                    { "intents", "true" },
                    { "sentiment", "true" },
                    { "language", languageCode },
                };
                var queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={x.Value}"));
                request.RequestUri = new Uri($"{apiUrls["Deepgram"]}?{queryString}");

                Log($"Deepgram request payload: {jsonBody}");
                Log($"Deepgram request URL: {request.RequestUri}");

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                Log($"Deepgram full response: {responseBody}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var deepgramResponse = JsonSerializer.Deserialize<DeepgramResponse>(
                    responseBody,
                    options
                );

                if (deepgramResponse?.Results?.Summary?.Text == null)
                {
                    throw new InvalidOperationException(
                        "Failed to deserialize Deepgram response or summary is missing"
                    );
                }

                var summary = deepgramResponse.Results.Summary.Text;
                Log($"Parsed summary: {summary}");
                return summary;
            }
            catch (Exception ex)
            {
                LogError("Deepgram", ex);
                throw;
            }
        }

        public async Task<EdenaiResponse> EdenaiAsync(EdenaiComponents components)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, apiUrls["Edenai"]);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {apiTokens["Edenai"]}");

                var jsonBody = JsonSerializer.Serialize(
                    new
                    {
                        response_as_dict = true,
                        attributes_as_list = false,
                        show_base_64 = true,
                        show_original_response = false,
                        text = components.Text,
                        providers = components.Providers,
                        language = components.Language,
                        output_sentences = components.OutputSentences,
                    }
                );

                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                Log($"Edenai request payload: {jsonBody}");

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                Log($"Edenai full response: {responseBody}");

                var result = JsonSerializer.Deserialize<Dictionary<string, EdenaiResponse>>(
                    responseBody
                );
                if (
                    result == null
                    || !result.TryGetValue(components.Providers ?? "", out var edenaiResponse)
                )
                {
                    throw new InvalidOperationException("Failed to deserialize Edenai response");
                }
                Log($"Parsed result: {edenaiResponse.Result}, Cost: {edenaiResponse.Cost}");
                return edenaiResponse;
            }
            catch (Exception ex)
            {
                LogError("Edenai", ex);
                throw;
            }
        }

        private void LogError(string provider, Exception ex)
        {
            Log($"Error in {provider} summarization: {ex.Message}");
        }

        private void Log(object info)
        {
            if (!logger)
                return;

            string message = info switch
            {
                EdenaiResponse edenaiResponse =>
                    $"Results: {edenaiResponse.Result}\nCosts: {edenaiResponse.Cost}",
                _ => $"Results: {info}",
            };

            Console.WriteLine($"[DEBUG SummarizeText]\n* {message}");
        }
    }
}
