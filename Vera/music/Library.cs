using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ConsoleTableExt;
using NAudio.Wave;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace Vera.Music
{
    /// <summary>
    /// Provides functionality for searching, downloading, and playing YouTube music.
    /// </summary>
    public partial class YouTubeMusic
    {
        private readonly YoutubeDL _youtubeDL;
        private readonly string _outputFolder;
        private static VideoInfo? _selectedVideo;
        private static WaveOutEvent? _currentOutputDevice;
        private static float _currentVolume = 0.2f;
        private static readonly Regex _youtubeUrlRegex = YouTubeRegex();
        private static readonly ConcurrentDictionary<string, VideoInfo> _videoInfoCache = new();

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
                return IsYouTubeUrl(query)
                    ? await SearchSingleVideo(query)
                    : await SearchMultipleVideos(query, maxResults);
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
            return videoInfo != null ? [videoInfo] : [];
        }

        private async Task<List<VideoInfo>> SearchMultipleVideos(string query, int maxResults)
        {
            var searchResult = await _youtubeDL.RunVideoDataFetch(
                $"ytsearch{maxResults}:{query}",
                CancellationToken.None
            );

            if (!searchResult.Success || searchResult.Data?.Entries == null)
                return [];

            var tasks = searchResult
                .Data.Entries.AsParallel()
                .Select(async video => await GetVideoInfoFromUrl(video.ID))
                .ToList();

            var results = await Task.WhenAll(tasks);
            return results.Where(v => v != null).ToList()!;
        }

        private static bool IsYouTubeUrl(string query) => _youtubeUrlRegex.IsMatch(query);

        private async Task<VideoInfo?> GetVideoInfoFromUrl(string url)
        {
            if (_videoInfoCache.TryGetValue(url, out var cachedInfo))
                return cachedInfo;

            try
            {
                var result = await _youtubeDL.RunVideoDataFetch(url);

                if (!result.Success || result.Data == null)
                    return null;

                var audioFormat = result.Data.Formats?.LastOrDefault(f =>
                    f?.Resolution == "audio only"
                );

                var videoInfo = new VideoInfo(
                    result.Data.Title ?? "Unknown Title",
                    result.Data.Uploader ?? "Unknown Uploader",
                    url,
                    audioFormat?.Url ?? string.Empty
                );

                _videoInfoCache.TryAdd(url, videoInfo);
                return videoInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while fetching video info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Downloads and plays the audio of the specified video.
        /// </summary>
        /// <param name="video">The VideoInfo object representing the video to play.</param>
        /// <returns>
        /// A Task representing the asynchronous operation of downloading and playing the audio.
        /// </returns>
        public async Task Play(VideoInfo video)
        {
            try
            {
                _selectedVideo = video;
                var filePath = Path.Combine(_outputFolder, $"{video.Url}.mp3");

                if (!File.Exists(filePath))
                {
                    var res = await _youtubeDL.RunAudioDownload(
                        video.Url,
                        AudioConversionFormat.Mp3
                    );
                    if (!res.Success || res.Data == null || res.Data.Length == 0)
                    {
                        Console.WriteLine("Failed to download the audio or no data returned.");
                        return;
                    }
                }

                await PlayAudioAsync(filePath);
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
        /// <returns>A Task representing the asynchronous operation.</returns>
        public static async Task PlayAudioFromUrl(
            string url,
            float volume = 0.2f,
            object? videoInfo = null
        )
        {
            try
            {
                _selectedVideo = videoInfo as VideoInfo;
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
            using (_currentOutputDevice = new WaveOutEvent())
            {
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
        }

        private static void HandlePlaybackException(Exception ex)
        {
            Console.WriteLine($"Error during URL audio playback: {ex.Message}");
            if (ex is ArgumentException)
                Console.WriteLine("The provided URL might be invalid or not supported.");
            else if (ex is InvalidOperationException)
                Console.WriteLine(
                    "Failed to initialize the audio stream. Please check the URL and try again."
                );
            else
                Console.WriteLine("An unexpected error occurred. Please try again later.");
        }

        private static void PrintPlaybackInfo()
        {
            if (_selectedVideo == null)
            {
                Console.WriteLine("No video information available.");
                return;
            }

            string videoUrl = _youtubeUrlRegex.IsMatch(_selectedVideo.Url)
                ? _selectedVideo.Url
                : $"https://www.youtube.com/watch?v={_selectedVideo.Url}";

            var tableData = new List<List<object>>
            {
                new() { "Title", _selectedVideo.Title },
                new() { "Artist", _selectedVideo.Artist },
                new() { "Video URL", $"{videoUrl} (ID: {_selectedVideo.Url})" },
                new()
                {
                    "Controls",
                    "'P' or Space to pause/resume, 'V' to adjust volume, 'Q' to quit",
                },
            };

            ConsoleTableBuilder
                .From(tableData)
                .WithCharMapDefinition(
                    CharMapDefinition.FramePipDefinition,
                    new Dictionary<HeaderCharMapPositions, char>
                    {
                        { HeaderCharMapPositions.TopLeft, '╒' },
                        { HeaderCharMapPositions.TopCenter, '╤' },
                        { HeaderCharMapPositions.TopRight, '╕' },
                        { HeaderCharMapPositions.BottomLeft, '╞' },
                        { HeaderCharMapPositions.BottomCenter, '╪' },
                        { HeaderCharMapPositions.BottomRight, '╡' },
                        { HeaderCharMapPositions.BorderTop, '═' },
                        { HeaderCharMapPositions.BorderRight, '│' },
                        { HeaderCharMapPositions.BorderLeft, '│' },
                        { HeaderCharMapPositions.Divider, '│' },
                    }
                )
                .ExportAndWriteLine(TableAligntment.Left);
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
            if (!Console.KeyAvailable)
                return false;

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
            return false;
        }

        private static void TogglePlayPause(WaveOutEvent outputDevice)
        {
            if (outputDevice.PlaybackState == PlaybackState.Playing)
                outputDevice.Pause();
            else if (outputDevice.PlaybackState == PlaybackState.Paused)
                outputDevice.Play();
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
        }

        private static string FormatDuration(TimeSpan duration) =>
            $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}";

        [GeneratedRegex(
            @"^(https?:\/\/)?(www\.)?(youtube\.com|youtu\.?be)\/.+$",
            RegexOptions.Compiled
        )]
        private static partial Regex YouTubeRegex();
    }

    /// <summary>
    /// Represents information about a YouTube video.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="VideoInfo"/> class.
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
        /// <value>
        /// The title of the video as a string.
        /// </value>
        public string Title { get; } = title;

        /// <summary>
        /// Gets the artist or uploader of the video.
        /// </summary>
        /// <value>
        /// The artist or uploader of the video as a string.
        /// </value>
        public string Artist { get; } = artist;

        /// <summary>
        /// Gets the URL of the video.
        /// </summary>
        /// <value>
        /// The URL of the video as a string.
        /// </value>
        public string Url { get; } = url;

        /// <summary>
        /// Gets the URL of the audio stream for the video.
        /// </summary>
        /// <value>
        /// The URL of the audio stream for the video as a string.
        /// </value>
        public string AudioStreamUrl { get; } = audioStreamUrl;
    }
}
