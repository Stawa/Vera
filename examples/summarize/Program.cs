using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GTTS.Summarize;

class Program
{
    static async Task Main(string[] args)
    {
        var apiTokens = new Dictionary<string, string>
        {
            { "Deepgram", "DEEPGRAM_API_KEY" },
            { "Edenai", "EDENAI_API_KEY" },
        };

        using var httpClient = new HttpClient();
        var summarizer = new SummarizeText(apiTokens, httpClient, logger: false);

        try
        {
            string textToSummarize =
                "Artificial Intelligence (AI) is a branch of computer science that aims to create intelligent machines that can perform tasks that typically require human intelligence. These tasks include visual perception, speech recognition, decision-making, and language translation. AI systems are designed to learn from experience, adjust to new inputs, and perform human-like tasks. The field of AI is interdisciplinary, drawing from computer science, cognitive science, linguistics, psychology, and other fields. As AI continues to advance, it has the potential to revolutionize various industries and aspects of daily life, from healthcare and education to transportation and entertainment.";

            Console.WriteLine("Summarizing with Deepgram:");
            var deepgramSummary = await summarizer.DeepgramAsync(textToSummarize, "en");
            Console.WriteLine($"Deepgram summary: {deepgramSummary}\n");

            Console.WriteLine("Summarizing with Edenai:");
            var edenaiResponse = await summarizer.EdenaiAsync(
                new EdenaiComponents
                {
                    Text = textToSummarize,
                    Providers = "openai",
                    Language = "en",
                    OutputSentences = 3,
                }
            );
            Console.WriteLine($"Edenai summary: {edenaiResponse.Result}");
            Console.WriteLine($"Edenai cost: {edenaiResponse.Cost}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
