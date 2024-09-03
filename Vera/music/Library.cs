using System.Text.RegularExpressions;
using NAudio.Wave;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace Vera.Music
{
    public class YouTubeMusic
    {
        private readonly YoutubeDL _youtubeDL;
        private readonly string _outputFolder;
        private static VideoInfo? _selectedVideo;
        private static WaveOutEvent? _currentOutputDevice;
        private static float _currentVolume = 0.2f;

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

        public async Task<object> Search(string query, int maxResults = 5)
        {
            try
            {
                if (IsYouTubeUrl(query))
                {
                    return await SearchSingleVideo(query);
                }
                else
                {
                    return await SearchMultipleVideos(query, maxResults);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during search: {ex.Message}");
                return new List<VideoInfo>();
            }
        }

        private async Task<List<VideoInfo>> SearchSingleVideo(string query)
        {
            var videoInfo = await GetVideoInfoFromUrl(query);
            return videoInfo != null ? [videoInfo] : [];
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
                foreach (var video in searchResult.Data.Entries)
                {
                    var fetchedVideo = await GetVideoInfoFromUrl(video.ID);
                    if (fetchedVideo != null)
                    {
                        results.Add(fetchedVideo);
                    }
                }
            }
            Console.WriteLine($"Found {results.Count} results for query: {query}");
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
                var result = await _youtubeDL.RunVideoDataFetch(url);

                if (result.Success && result.Data != null)
                {
                    var audioFormat = result
                        .Data.Formats?.Where(f => f?.Resolution == "audio only")
                        .LastOrDefault();

                    return new VideoInfo(
                        result.Data.Title ?? "Unknown Title",
                        result.Data.Uploader ?? "Unknown Uploader",
                        url,
                        audioFormat?.Url ?? string.Empty
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching video info: {ex.Message}");
            }
            return null;
        }

        public async Task Play(VideoInfo video)
        {
            try
            {
                _selectedVideo = video;
                var res = await _youtubeDL.RunAudioDownload(video.Url, AudioConversionFormat.Mp3);
                if (res.Success && res.Data != null && res.Data.Length > 0)
                {
                    string filePath = Path.Combine(_outputFolder, "output.mp3");
                    Console.WriteLine($"Audio file downloaded to: {filePath}");
                    await PlayAudioAsync(filePath);
                }
                else
                {
                    Console.WriteLine("Failed to download the audio or no data returned.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during playback: {ex.Message}");
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
                Console.WriteLine($"An error occurred during playback: {ex.Message}");
            }
        }

        public static async Task PlayAudioFromUrl(string url, float volume = 0.2f)
        {
            Console.WriteLine($"Playing audio from URL: {url}");
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
                "Press 'P' or Space to pause/resume, 'V' to adjust volume, 'Q' to quit"
            );

            await UpdatePlaybackProgressAsync(_currentOutputDevice, reader, duration);

            Console.WriteLine("\nPlayback finished.");
        }

        private static void HandlePlaybackException(Exception ex)
        {
            Console.WriteLine($"An error occurred during URL audio playback: {ex.Message}");
            if (ex is ArgumentException)
            {
                Console.WriteLine("The URL provided might be invalid or not supported.");
            }
            else if (ex is InvalidOperationException)
            {
                Console.WriteLine(
                    "The audio stream could not be initialized. Please check the URL and try again."
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

            Console.WriteLine("Playback Information:");
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
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }

    public class VideoInfo(string title, string artist, string url, string audioStreamUrl)
    {
        public string Title { get; } = title;
        public string Artist { get; } = artist;
        public string Url { get; } = url;
        public string AudioStreamUrl { get; } = audioStreamUrl;
    }
}
