using NAudio.Wave;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace Vera.Music
{
    public class YouTubeMusic
    {
        private readonly YoutubeDL _youtubeDL;
        private readonly string _outputFolder;

        private static (string Title, string Artist, string Url) _selectedVideo;

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

        public async Task<List<(string Title, string Url)>> Search(string query)
        {
            var results = new List<(string Title, string Url)>();
            try
            {
                var searchResult = await _youtubeDL.RunVideoDataFetch($"ytsearch10:{query}");
                if (searchResult.Success && searchResult.Data != null)
                {
                    foreach (var video in searchResult.Data.Entries)
                    {
                        string title = video.Title;
                        string url = video.Url;
                        results.Add((title, url));
                        _selectedVideo = (title, video.Uploader, url);
                    }
                }

                Console.WriteLine($"Found {results.Count} results for query: {query}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return results;
        }

        public async Task Play(string url)
        {
            try
            {
                var res = await _youtubeDL.RunAudioDownload(url, AudioConversionFormat.Mp3);
                Console.WriteLine($"Downloaded: {res}");

                if (res.Success && res.Data != null && res.Data.Length > 0)
                {
                    string filePath = Path.Combine(_outputFolder, $"output.mp3");
                    Console.WriteLine($"Audio file downloaded to: {filePath}");
                    PlayAudio(filePath);
                }
                else
                {
                    Console.WriteLine("Failed to download the audio or no data returned.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void PlayAudio(string filePath, float volume = 0.2f)
        {
            try
            {
                using var audioFile = new MediaFoundationReader(filePath);
                var volumeProvider = new VolumeWaveProvider16(audioFile) { Volume = volume };

                using var outputDevice = new WaveOutEvent();
                outputDevice.Init(volumeProvider);

                var duration = audioFile.TotalTime;
                var formattedDuration = $"{duration.Minutes:D2}:{duration.Seconds:D2}";

                Console.WriteLine("Playing audio...");
                Console.WriteLine($"Title: {_selectedVideo.Title}");
                Console.WriteLine($"Artist: {_selectedVideo.Artist}");
                Console.WriteLine($"Video URL: {_selectedVideo.Url}");
                Console.WriteLine($"Duration: 00:00/{formattedDuration}");

                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    var currentPosition = outputDevice.GetPosition();
                    var currentDuration = TimeSpan.FromMilliseconds(
                        currentPosition / (audioFile.WaveFormat.AverageBytesPerSecond / 1000.0)
                    );

                    var formattedCurrentPosition =
                        $"{currentDuration.Minutes:D2}:{currentDuration.Seconds:D2}";

                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine($"Duration: {formattedCurrentPosition}/{formattedDuration}");

                    Thread.Sleep(1000);
                }

                Console.WriteLine("\nPlayback finished.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during playback: {ex.Message}");
            }
        }
    }
}
