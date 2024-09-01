using System.Text.Json.Serialization;

namespace Vera.Summarize
{
    public class DeepgramResponse
    {
        [JsonPropertyName("results")]
        public DeepgramResults? Results { get; set; }
    }

    public class DeepgramResults
    {
        [JsonPropertyName("summary")]
        public DeepgramSummary? Summary { get; set; }
    }

    public class DeepgramSummary
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class EdenaiResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class EdenaiComponents
    {
        public string? Text { get; set; }
        public string? Providers { get; set; }
        public string? Language { get; set; }
        public int OutputSentences { get; set; }
    }
}
