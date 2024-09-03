using System.Text.Json.Serialization;

namespace Vera.Summarize
{
    /// <summary>
    /// Represents the response from the Deepgram API.
    /// </summary>
    public class DeepgramResponse
    {
        /// <summary>
        /// Gets or sets the results of the Deepgram API response.
        /// </summary>
        [JsonPropertyName("results")]
        public DeepgramResults? Results { get; set; }
    }

    /// <summary>
    /// Represents the results section of the Deepgram API response.
    /// </summary>
    public class DeepgramResults
    {
        /// <summary>
        /// Gets or sets the summary of the Deepgram API results.
        /// </summary>
        [JsonPropertyName("summary")]
        public DeepgramSummary? Summary { get; set; }
    }

    /// <summary>
    /// Represents the summary section of the Deepgram API results.
    /// </summary>
    public class DeepgramSummary
    {
        /// <summary>
        /// Gets or sets the summarized text from the Deepgram API.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>
    /// Represents the response from the Edenai API.
    /// </summary>
    public class EdenaiResponse
    {
        /// <summary>
        /// Gets or sets the result of the Edenai API response.
        /// </summary>
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        /// <summary>
        /// Gets or sets the cost associated with the Edenai API request.
        /// </summary>
        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }

        /// <summary>
        /// Gets or sets the status of the Edenai API response.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// Represents the components of an Edenai API request.
    /// </summary>
    public class EdenaiComponents
    {
        /// <summary>
        /// Gets or sets the text to be summarized.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the providers to be used for summarization.
        /// </summary>
        public string? Providers { get; set; }

        /// <summary>
        /// Gets or sets the language of the text to be summarized.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the number of sentences in the output summary.
        /// </summary>
        public int OutputSentences { get; set; }
    }
}
