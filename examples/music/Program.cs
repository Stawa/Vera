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

        var searchResult = await youtubeMusic.Search(query, 1);

        if (searchResult is List<VideoInfo> results)
        {
            if (results.Count == 0)
            {
                Console.WriteLine("No results found.");
                return;
            }

            if (results.Count == 1)
            {
                await PlayVideo(results[0]);
            }
            else
            {
                await HandleMultipleResults(results);
            }
        }
        else
        {
            Console.WriteLine("An error occurred during the search.");
        }
    }

    private static async Task PlayVideo(VideoInfo video)
    {
        Console.WriteLine($"Playing: {video.Title} by {video.Artist}");
        await YouTubeMusic.PlayAudioFromUrl(video.AudioStreamUrl, 0.2f, video);
    }

    private static async Task HandleMultipleResults(List<VideoInfo> results)
    {
        Console.WriteLine("Search Results:");
        for (int i = 0; i < results.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {results[i].Title} by {results[i].Artist}");
        }

        Console.Write("Enter the number of the song you want to play: ");
        if (
            int.TryParse(Console.ReadLine(), out int choice)
            && choice > 0
            && choice <= results.Count
        )
        {
            await PlayVideo(results[choice - 1]);
        }
        else
        {
            Console.WriteLine("Invalid choice.");
        }
    }
}
