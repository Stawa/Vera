using System.Text;
using NAudio.Wave;

namespace Vera.Voice
{
    public class RealtimeMicrophoneRecognition : IDisposable
    {
        private readonly IRecognitionEngine _recognitionEngine;
        private readonly WaveInEvent _waveIn;
        private readonly CancellationTokenSource _cts = new();
        private readonly MemoryStream _audioBuffer = new();
        private readonly object _bufferLock = new();
        private readonly Timer _recognitionTimer;
        private readonly Timer _inactivityTimer;
        private readonly StringBuilder _accumulatedText = new();

        public event EventHandler<string>? ResultResponse;
        public event EventHandler<string>? SpeechEndResult;

        public RealtimeMicrophoneRecognition(
            string modelPath,
            int sampleRate = 16000,
            int channels = 1
        )
        {
            _recognitionEngine = RecognitionEngineFactory.CreateEngine(modelPath);
            _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(sampleRate, channels) };
            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _recognitionTimer = new Timer(RecognizeSpeechFromBuffer, null, 1000, 1000);
            _inactivityTimer = new Timer(
                OnInactivityDetected,
                null,
                Timeout.Infinite,
                Timeout.Infinite
            );
        }

        public async Task StartAsync()
        {
            _waveIn.StartRecording();
            Console.WriteLine("Listening... Press any key to stop.");
            await Task.Run(Console.ReadKey);
            await StopAsync();
        }

        public Task StopAsync()
        {
            _waveIn.StopRecording();
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            lock (_bufferLock)
            {
                _audioBuffer.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        private async void RecognizeSpeechFromBuffer(object? state)
        {
            byte[] audioData;
            lock (_bufferLock)
            {
                audioData = _audioBuffer.ToArray();
                _audioBuffer.SetLength(0);
            }

            if (audioData.Length > 0)
            {
                using var stream = new MemoryStream(audioData);
                try
                {
                    string result = await _recognitionEngine.RecognizeSpeechAsync(stream);
                    if (string.IsNullOrEmpty(result))
                    {
                        Console.WriteLine("No speech recognized.");
                    }
                    else
                    {
                        lock (_accumulatedText)
                        {
                            _accumulatedText.Append(result);
                            string accumulatedText = _accumulatedText.ToString();
                            ResultResponse?.Invoke(this, accumulatedText);
                        }
                        _inactivityTimer.Change(2000, Timeout.Infinite); // Reset inactivity timer
                    }
                }
                catch (Exception ex) when (!_cts.IsCancellationRequested)
                {
                    Console.WriteLine($"Recognition error: {ex.Message}");
                }
            }
        }

        private void OnInactivityDetected(object? state)
        {
            lock (_accumulatedText)
            {
                string finalText = _accumulatedText.ToString();
                if (!string.IsNullOrEmpty(finalText))
                {
                    SpeechEndResult?.Invoke(this, finalText);
                    _accumulatedText.Clear();
                }
            }
        }

        public void Dispose()
        {
            _waveIn.Dispose();
            _cts.Dispose();
            _recognitionTimer.Dispose();
            _inactivityTimer.Dispose();
            GC.SuppressFinalize(this);
        }

        public static List<MicrophoneInfo> ListMicrophones()
        {
            var microphones = new List<MicrophoneInfo>();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var capabilities = WaveInEvent.GetCapabilities(i);
                microphones.Add(
                    new MicrophoneInfo
                    {
                        Index = i,
                        Name = capabilities.ProductName.TrimEnd('\0'),
                        Channels = capabilities.Channels,
                    }
                );
            }
            return microphones;
        }
    }

    public class MicrophoneInfo
    {
        public required int Index { get; set; }
        public required string Name { get; set; }
        public required int Channels { get; set; }
    }
}
