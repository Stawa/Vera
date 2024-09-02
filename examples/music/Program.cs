using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vera.Music;

class Program
{
    static async Task Main(string[] args)
    {
        var youtubeMusic = new YouTubeMusic(
            "C:\\Users\\Administrator\\Desktop\\Vera Core\\examples\\music\\yt-dlp.exe",
            "C:\\FFmpeg\\ffmpeg.exe",
            "output"
        );

        Console.Write("Enter a song or artist to search for: ");
        string query = Console.ReadLine() ?? string.Empty;

        var results = await youtubeMusic.Search(query);

        if (results.Count == 0)
        {
            Console.WriteLine("No results found.");
            return;
        }

        Console.WriteLine("Search Results:");
        for (int i = 0; i < results.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {results[i].Title} - {results[i].Url}");
        }

        Console.Write("Enter the number of the song you want to play: ");
        if (
            int.TryParse(Console.ReadLine(), out int choice)
            && choice > 0
            && choice <= results.Count
        )
        {
            string url = results[choice - 1].Url;
            await youtubeMusic.Play(url);
        }
        else
        {
            Console.WriteLine("Invalid choice.");
        }
    }
}
