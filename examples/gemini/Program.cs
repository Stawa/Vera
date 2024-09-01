using System;
using System.Threading.Tasks;
using Vera;

class Program
{
    static async Task Main(string[] args)
    {
        string apiKey = "GEMINI_API_KEY";
        var gemini = new Gemini(apiKey, Gemini.GeminiModel.Gemini15FlashLatest);

        try
        {
            string prompt = "Explain the concept of artificial intelligence in simple terms.";

            Console.WriteLine("Sending prompt to Gemini API...");
            string response = await gemini.FetchResponseAsync(prompt);

            Console.WriteLine("Response from Gemini:");
            Console.WriteLine(response);
        }
        catch (GeminiApiException ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
