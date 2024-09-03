using System.Text.RegularExpressions;
using NAudio.Wave;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace Vera.Music
{
    /// <summary>
    /// Provides functionality for searching, downloading, and playing YouTube music.
    /// </summary>
    public class YouTubeMusic
    {
        private readonly YoutubeDL _youtubeDL;
        private readonly string _outputFolder;
        private static VideoInfo? _selectedVideo;
        private static WaveOutEvent? _currentOutputDevice;
        private static float _currentVolume = 0.2f;

        /// <summary>
        /// Initializes a new instance of the YouTubeMusic class.
        /// </summary>
        /// <param name="youtubeDLPath">The path to the youtube-dl executable.</param>
        /// <param name="ffmpegPath">The path to the ffmpeg executable.</param>
        /// <param name="outputFolder">The folder where downloaded audio files will be saved.</param>
        public YouTubeMusic(string youtubeDLPath, string ffmpegPath, string outputFolder)
        {
            _youtubeDL = new YoutubeDL
            {
                YoutubeDLPath = youtubeDLPath,
                FFmpegPath = ffmpegPath,
                OutputFolder = outputFolder,
                OutputFileTemplate = "output",
            };
            _outputFolder = outputFolder;
        }

        /// <summary>
        /// Searches for YouTube videos based on the provided query.
        /// </summary>
        /// <param name="query">The search query or YouTube URL.</param>
        /// <param name="maxResults">The maximum number of results to return (default is 5).</param>
        /// <returns>A list of VideoInfo objects representing the search results.</returns>
        public async Task<object> Search(string query, int maxResults = 5)
        {
            try
            {
                if (IsYouTubeUrl(query))
                {
                    Console.WriteLine($"Searching for single video: {query}");
                    return await SearchSingleVideo(query);
                }
                else
                {
                    Console.WriteLine($"Searching for '{query}' (max results: {maxResults})");
                    return await SearchMultipleVideos(query, maxResults);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during search: {ex.Message}");
                return new List<VideoInfo>();
            }
        }

        private async Task<List<VideoInfo>> SearchSingleVideo(string query)
        {
            var videoInfo = await GetVideoInfoFromUrl(query);
            _selectedVideo = videoInfo;
            if (videoInfo != null)
            {
                Console.WriteLine($"Found video: {videoInfo.Title} by {videoInfo.Artist}");
                return [videoInfo];
            }
            Console.WriteLine("No video found for the given URL.");
            return [];
        }

        private async Task<List<VideoInfo>> SearchMultipleVideos(string query, int maxResults)
        {
            var results = new List<VideoInfo>();
            var searchResult = await _youtubeDL.RunVideoDataFetch(
                $"ytsearch{maxResults}:{query}",
                CancellationToken.None
            );

            if (searchResult.Success && searchResult.Data?.Entries != null)
            {
                Console.WriteLine($"Found {searchResult.Data.Entries.Length} initial results.");
                foreach (var video in searchResult.Data.Entries)
                {
                    var fetchedVideo = await GetVideoInfoFromUrl(video.ID);
                    if (fetchedVideo != null)
                    {
                        results.Add(fetchedVideo);
                        Console.WriteLine($"- {fetchedVideo.Title} by {fetchedVideo.Artist}");
                    }
                }
            }
            Console.WriteLine($"Retrieved {results.Count} valid results for query: {query}");
            return results;
        }

        private static bool IsYouTubeUrl(string query)
        {
            string pattern = @"^(https?:\/\/)?(www\.)?(youtube\.com|youtu\.?be)\/.+$";
            return Regex.IsMatch(query, pattern);
        }

        private async Task<VideoInfo?> GetVideoInfoFromUrl(string url)
        {
            try
            {
                Console.WriteLine($"Fetching video info for: {url}");
                var result = await _youtubeDL.RunVideoDataFetch(url);

                if (result.Success && result.Data != null)
                {
                    var audioFormat = result
                        .Data.Formats?.Where(f => f?.Resolution == "audio only")
                        .LastOrDefault();

                    var videoInfo = new VideoInfo(
                        result.Data.Title ?? "Unknown Title",
                        result.Data.Uploader ?? "Unknown Uploader",
                        url,
                        audioFormat?.Url ?? string.Empty
                    );
                    Console.WriteLine(
                        $"Video info retrieved: {videoInfo.Title} by {videoInfo.Artist}"
                    );
                    return videoInfo;
                }
                Console.WriteLine("Failed to fetch video info.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while fetching video info: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Downloads and plays the audio of the specified video.
        /// </summary>
        /// <param name="video">The VideoInfo object representing the video to play.</param>
        public async Task Play(VideoInfo video)
        {
            try
            {
                _selectedVideo = video;
                Console.WriteLine($"Downloading audio for: {video.Title}");
                var res = await _youtubeDL.RunAudioDownload(video.Url, AudioConversionFormat.Mp3);
                if (res.Success && res.Data != null && res.Data.Length > 0)
                {
                    string filePath = Path.Combine(_outputFolder, "output.mp3");
                    Console.WriteLine($"Audio file downloaded successfully to: {filePath}");
                    await PlayAudioAsync(filePath);
                }
                else
                {
                    Console.WriteLine("Failed to download the audio or no data returned.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during playback: {ex.Message}");
            }
        }

        private static async Task PlayAudioAsync(string filePath, float volume = 0.2f)
        {
            try
            {
                Console.WriteLine($"Initializing playback from file: {filePath}");
                using var audioFile = new MediaFoundationReader(filePath);
                await PlayAudioFromReader(audioFile, volume);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file playback: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays audio from the specified URL.
        /// </summary>
        /// <param name="url">The URL of the audio to play.</param>
        /// <param name="volume">The initial volume (0.0 to 1.0, default is 0.2).</param>
        public static async Task PlayAudioFromUrl(string url, float volume = 0.2f)
        {
            Console.WriteLine($"Initializing playback from URL: {url}");
            try
            {
                using var mf = new MediaFoundationReader(url);
                await PlayAudioFromReader(mf, volume);
            }
            catch (Exception ex)
            {
                HandlePlaybackException(ex);
            }
        }

        private static async Task PlayAudioFromReader(MediaFoundationReader reader, float volume)
        {
            var volumeProvider = new VolumeWaveProvider16(reader) { Volume = volume };
            _currentOutputDevice = new WaveOutEvent();
            _currentOutputDevice.Init(volumeProvider);
            _currentOutputDevice.Play();

            var duration = reader.TotalTime;
            PrintPlaybackInfo();
            Console.WriteLine($"Total duration: {FormatDuration(duration)}");
            Console.WriteLine(
                "Controls: 'P' or Space to pause/resume, 'V' to adjust volume, 'Q' to quit"
            );

            await UpdatePlaybackProgressAsync(_currentOutputDevice, reader, duration);

            Console.WriteLine("\nPlayback finished.");
        }

        private static void HandlePlaybackException(Exception ex)
        {
            Console.WriteLine($"Error during URL audio playback: {ex.Message}");
            if (ex is ArgumentException)
            {
                Console.WriteLine("The provided URL might be invalid or not supported.");
            }
            else if (ex is InvalidOperationException)
            {
                Console.WriteLine(
                    "Failed to initialize the audio stream. Please check the URL and try again."
                );
            }
            else
            {
                Console.WriteLine("An unexpected error occurred. Please try again later.");
            }
        }

        private static void PrintPlaybackInfo()
        {
            if (_selectedVideo == null)
            {
                Console.WriteLine("No video information available.");
                return;
            }

            Console.WriteLine("Now Playing:");
            Console.WriteLine($"Title: {_selectedVideo.Title}");
            Console.WriteLine($"Artist: {_selectedVideo.Artist}");
            Console.WriteLine($"Video URL: {_selectedVideo.Url}");
            Console.WriteLine($"Audio Stream URL: {_selectedVideo.AudioStreamUrl}");
        }

        private static async Task UpdatePlaybackProgressAsync(
            WaveOutEvent outputDevice,
            MediaFoundationReader audioFile,
            TimeSpan duration
        )
        {
            while (outputDevice.PlaybackState != PlaybackState.Stopped)
            {
                UpdateProgressDisplay(outputDevice, audioFile, duration);
                if (await HandleUserInput(outputDevice))
                    return;
                await Task.Delay(1000);
            }
        }

        private static void UpdateProgressDisplay(
            WaveOutEvent outputDevice,
            MediaFoundationReader audioFile,
            TimeSpan duration
        )
        {
            var currentPosition = outputDevice.GetPosition();
            var currentDuration = TimeSpan.FromMilliseconds(
                currentPosition / (audioFile.WaveFormat.AverageBytesPerSecond / 1000.0)
            );

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(
                $"\rDuration: {FormatDuration(currentDuration)}/{FormatDuration(duration)} | Volume: {_currentVolume:P0}".PadRight(
                    Console.WindowWidth
                )
            );
        }

        private static async Task<bool> HandleUserInput(WaveOutEvent outputDevice)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.P:
                    case ConsoleKey.Spacebar:
                        TogglePlayPause(outputDevice);
                        break;
                    case ConsoleKey.V:
                        await AdjustVolumeAsync(outputDevice);
                        break;
                    case ConsoleKey.Q:
                        outputDevice.Stop();
                        return true;
                }
            }
            return false;
        }

        private static void TogglePlayPause(WaveOutEvent outputDevice)
        {
            if (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Pause();
            }
            else if (outputDevice.PlaybackState == PlaybackState.Paused)
            {
                outputDevice.Play();
            }
        }

        private static async Task AdjustVolumeAsync(WaveOutEvent outputDevice)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("\rEnter new volume (0-100): ");
            string? input = await Task.Run(Console.ReadLine);
            if (int.TryParse(input, out int newVolume) && newVolume >= 0 && newVolume <= 100)
            {
                _currentVolume = newVolume / 100f;
                outputDevice.Volume = _currentVolume;
            }
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }

    /// <summary>
    /// Represents information about a YouTube video.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the VideoInfo class.
    /// </remarks>
    /// <param name="title">The title of the video.</param>
    /// <param name="artist">The artist or uploader of the video.</param>
    /// <param name="url">The URL of the video.</param>
    /// <param name="audioStreamUrl">The URL of the audio stream for the video.</param>
    public class VideoInfo(string title, string artist, string url, string audioStreamUrl)
    {
        /// <summary>
        /// Gets the title of the video.
        /// </summary>
        public string Title { get; } = title;

        /// <summary>
        /// Gets the artist or uploader of the video.
        /// </summary>
        public string Artist { get; } = artist;

        /// <summary>
        /// Gets the URL of the video.
        /// </summary>
        public string Url { get; } = url;

        /// <summary>
        /// Gets the URL of the audio stream for the video.
        /// </summary>
        public string AudioStreamUrl { get; } = audioStreamUrl;
    }
}
