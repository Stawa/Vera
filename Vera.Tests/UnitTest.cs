using System.Net;
using System.Text.Json;
using DotNetEnv;
using Moq;
using Moq.Protected;
using Vera;
using Vera.Summarize;
using Xunit.Abstractions;

namespace Vera.Tests
{
    public class GeminiTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _apiKey;
        private readonly Mock<HttpMessageHandler> _mockHandler;
        private readonly Gemini _gemini;

        public GeminiTests(ITestOutputHelper output)
        {
            _output = output;
            Env.Load();
            _apiKey =
                Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new InvalidOperationException(
                    "GEMINI_API_KEY environment variable is not set."
                );
            _mockHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockHandler.Object);
            _gemini = new Gemini(_apiKey, Gemini.GeminiModel.Gemini15FlashLatest, httpClient);
        }

        [Fact]
        public async Task FetchResponseAsync_ReturnsExpectedResponse()
        {
            const string expectedResponse = "Test response";
            SetupMockHandler(expectedResponse);

            _output.WriteLine("Sending request with prompt: Test prompt");
            var result = await _gemini.FetchResponseAsync("Test prompt");

            _output.WriteLine($"Received response: {result}");
            Assert.Equal(expectedResponse, result);
        }

        private void SetupMockHandler(string response)
        {
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            $"{{\"candidates\":[{{\"content\":{{\"parts\":[{{\"text\":\"{response}\"}}]}}}}]}}"
                        ),
                    }
                );
        }
    }

    public class SummarizeTextTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly SummarizeText _summarizeText;

        public SummarizeTextTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var client = new HttpClient(_mockHttpMessageHandler.Object);
            var apiTokens = new Dictionary<string, string>
            {
                { "Deepgram", "fake_deepgram_token" },
                { "Edenai", "fake_edenai_token" },
            };
            _summarizeText = new SummarizeText(apiTokens, client, logger: false);
        }

        [Fact]
        public async Task DeepgramAsync_ReturnsExpectedResponse()
        {
            var expectedResponse = new DeepgramResponse
            {
                Results = new DeepgramResults
                {
                    Summary = new DeepgramSummary { Text = "Test summary" },
                },
            };
            SetupMockResponse(JsonSerializer.Serialize(expectedResponse));

            var result = await _summarizeText.DeepgramAsync("Test text", "en");
            Assert.Equal("Test summary", result);
        }

        [Fact]
        public async Task EdenaiAsync_ReturnsExpectedResponse()
        {
            var expectedResponse = new Dictionary<string, EdenaiResponse>
            {
                {
                    "test_provider",
                    new EdenaiResponse { Result = "Test result", Cost = 0.1m }
                },
            };
            SetupMockResponse(JsonSerializer.Serialize(expectedResponse));

            var components = new EdenaiComponents
            {
                Text = "Test text",
                Providers = "test_provider",
                Language = "en",
                OutputSentences = 1,
            };
            var result = await _summarizeText.EdenaiAsync(components);

            Assert.Equal("Test result", result.Result);
            Assert.Equal(0.1m, result.Cost);
        }

        private void SetupMockResponse(string content)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(content),
                    }
                );
        }
    }
}
