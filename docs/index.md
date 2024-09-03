# 📖 Quick Start

![License](https://img.shields.io/github/license/Stawa/Vera?style=flat-square)
![Build](https://img.shields.io/github/actions/workflow/status/Stawa/Vera/dotnet.yml?style=flat-square)
[![Documentation](https://img.shields.io/badge/documentation-available-green?style=flat-square)](https://stawa.github.io/Vera)

Vera is a .NET package that converts textual content to speech, utilizing Google AI (Gemini) for text generation and internet-based information retrieval.

## 📚 Usage

Here's an example of using Vera to generate a response from the Gemini API:

```csharp
using Vera;
using System;
using System.Threading.Tasks;

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
```

Explore the [examples](https://github.com/Stawa/Vera/tree/main/examples) directory in this repository for more detailed examples and use cases. These examples highlight Vera's various capabilities and applications.

## 📜 License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/Stawa/Vera/blob/main/LICENSE) file for more details.
