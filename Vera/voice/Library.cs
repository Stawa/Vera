using Vosk;

namespace Vera.Voice
{
    public interface IVoiceRecognition : IDisposable
    {
        string RecognizeSpeech(byte[] audioData);
    }

    public class VoskVoiceRecognition : IVoiceRecognition
    {
        private readonly Model model;
        private readonly VoskRecognizer recognizer;

        public VoskVoiceRecognition(string modelPath)
        {
            Vosk.Vosk.SetLogLevel(0);
            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, 16000.0f);
        }

        public string RecognizeSpeech(byte[] audioData)
        {
            recognizer.AcceptWaveform(audioData, audioData.Length);
            return recognizer.Result();
        }

        public void Dispose()
        {
            recognizer?.Dispose();
            model?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public interface IRecognitionEngine
    {
        Task<string> RecognizeSpeechAsync(Stream audioStream);
    }

    public static class RecognitionEngineFactory
    {
        public static IRecognitionEngine CreateEngine(string modelPath)
        {
            return new VoskRecognitionEngine(modelPath);
        }
    }

    internal class VoskRecognitionEngine(string modelPath) : IRecognitionEngine
    {
        private readonly VoskVoiceRecognition voiceRecognition = new(modelPath);

        public async Task<string> RecognizeSpeechAsync(Stream audioStream)
        {
            using var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream);
            byte[] audioData = memoryStream.ToArray();
            return voiceRecognition.RecognizeSpeech(audioData);
        }
    }
}
