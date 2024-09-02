using System;
using System.Threading.Tasks;
using Vera.Voice;

class Program
{
    static async Task Main(string[] args)
    {
        string modelPath = "vosk-model-small-en-us-0.15";

        try
        {
            var microphones = RealtimeMicrophoneRecognition.ListMicrophones();
            Console.WriteLine("Available Microphones:");
            foreach (var mic in microphones)
            {
                Console.WriteLine($"{mic.Index}: {mic.Name} (Channels: {mic.Channels})");
            }

            Console.Write("Enter the index of the microphone you want to use: ");
            int selectedIndex = int.Parse(Console.ReadLine() ?? "0");

            using var recognition = new RealtimeMicrophoneRecognition(
                modelPath,
                16000,
                microphones[selectedIndex].Channels
            );
            recognition.ResultResponse += (sender, result) =>
            {
                Console.WriteLine($"Result: {result}");
            };
            recognition.SpeechEndResult += (sender, result) =>
            {
                Console.WriteLine($"Final Result: {result}");
            };

            Console.WriteLine("Starting recognition...");
            await recognition.StartAsync();
            Console.WriteLine("Recognition started.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
