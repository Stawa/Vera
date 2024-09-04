# 🎙️ Vera

![License](https://img.shields.io/github/license/Stawa/Vera?style=flat-square)
![Build](https://img.shields.io/github/actions/workflow/status/Stawa/Vera/dotnet.yml?style=flat-square)
[![Documentation](https://img.shields.io/badge/documentation-available-green?style=flat-square)](https://stawa.github.io/Vera/index.html)

Vera is a .NET package that converts textual content to speech, utilizing Google AI (Gemini) for text generation and internet-based information retrieval.

The original library was written in TypeScript ([GTTS](https://github.com/Stawa/GTTS)), and I'm now migrating the project to C# and .NET to make it faster and more efficient.

## 📦 Installation

To install the Vera package, you can use the NuGet package manager or install it directly from GitHub. Here are three methods:

1. Using the .NET CLI:
   Run the following command in your terminal:

   ```
   dotnet add package Vera
   ```

2. Using the Package Manager Console in Visual Studio:
   Execute this command:

   ```
   Install-Package Vera
   ```

3. Installing from GitHub:
   You can also install the latest version directly from the GitHub repository:

   ```
   dotnet add package Vera --version 1.0.0 --source https://nuget.pkg.github.com/Stawa/index.json
   ```

   Note: Replace '1.0.0' with the latest version number available on the GitHub repository.

After installation, you can start using Vera in your .NET projects by adding the appropriate using statements.

## 🚀 Usage

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

Explore the [examples](./examples) directory in this repository for more detailed examples and use cases. These examples highlight Vera's various capabilities and applications.

## 🚧 Project Status

The project is currently in the early stages of development, and the core functionality is implemented. However, the package is not yet fully tested and documented.

| Feature                   | Status         |
| ------------------------- | -------------- |
| Gemini API Integration    | ✅ Implemented |
| Text-to-Speech Conversion | ✅ Implemented |
| Error Handling            | ✅ Implemented |
| Testing                   | ✅ Implemented |
| Speech-to-Text Conversion | 🚧 On Progress |
| Voice Prompting           | ❌ Not Started |
| Switching Languages       | ❌ Not Started |
| Playing Music             | ✅ Implemented |
| Documentation             | ✅ Implemented |

In the future, I plan to create Vera Visual as an expansion of the V.E.R.A project. This function displays text for voice input and output on a small LCD screen, improving the system's user interface and interaction possibilities. Vera Visual seeks for a more accessible and user-friendly experience by combining visual feedback with vocal interactions.

## 🤝 Contributing

We welcome contributions to help improve Vera! If you discover any issues, have suggestions for improvements, or wish to add new features, please consider the following:

1. **Issues**: If you discover an error or have a feature request, please open an issue in our GitHub repository. Please provide as much detail as possible so that we can better understand and handle the issue.

2. **Pull Requests**: We appreciate pull requests for bug repairs, upgrades, and new features. Please take these steps:

   - Fork the repository and create a new branch with your changes.
   - Make your adjustments.
   - Submit a pull request with a comprehensive explanation of your changes.

3. **Documentation**: Enhancements to documentation are always welcome. If you see any areas that need explanation or have ideas for improved examples, please post a pull request.

4. **Code Style**: When contributing code, please follow the existing code style and conventions used in the project.

Your contributions help to improve Vera for everyone. Thank you for your support!

## 📜 License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for more details.
