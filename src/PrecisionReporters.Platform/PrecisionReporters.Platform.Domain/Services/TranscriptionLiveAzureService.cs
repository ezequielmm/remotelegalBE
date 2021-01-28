using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Domain.Configurations;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TranscriptionLiveAzureService : ITranscriptionLiveService
    {
        private const int ChannelCount = 1;
        private const int BitsPerSample = 16;
        private readonly AzureCognitiveServiceConfiguration _azureConfiguration;
        private readonly ITranscriptionService _transcriptionService;
        private PushAudioInputStream _audioInputStream;
        private SpeechRecognizer _recognizer;
        private readonly ConcurrentBuffer _recognizedBuffer = new ConcurrentBuffer();

        public TranscriptionLiveAzureService(IOptions<AzureCognitiveServiceConfiguration> azureConfiguration, ITranscriptionService transcriptionService)
        {
            _azureConfiguration = azureConfiguration.Value;
            _transcriptionService = transcriptionService;
        }

        public async Task<Transcription> RecognizeAsync(byte[] audioChunk, string userEmail, string depositionId, int sampleRate)
        {
            await InitializeRecognition(sampleRate);

            _audioInputStream.Write(audioChunk, audioChunk.Length);

            var recognizedText = _recognizedBuffer.GetResults();

            if (!string.IsNullOrWhiteSpace(recognizedText))
            {
                var transcription = new Transcription
                {
                    TranscriptDateTime = DateTime.UtcNow,
                    Text = recognizedText
                };
                
                await _transcriptionService.StoreTranscription(transcription, depositionId, userEmail);

                return transcription;
            }

            return new Transcription();
        }

        private async Task InitializeRecognition(int sampleRate)
        {
            if (_recognizer != null)
            {
                return;
            }

            var speechConfig = SpeechConfig.FromSubscription(_azureConfiguration.SubscriptionKey, _azureConfiguration.RegionCode);
            
            _audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(Convert.ToUInt16(sampleRate), BitsPerSample, ChannelCount));
            var audioConfig = AudioConfig.FromStreamInput(_audioInputStream);
            
            _recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            _recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    _recognizedBuffer.Append(e.Result.Text);
                }
            };

            await _recognizer.StartContinuousRecognitionAsync()
                             .ConfigureAwait(false);
        }

        internal class ConcurrentBuffer
        {
            private static readonly object _lock = new object();
            private readonly StringBuilder _buffer = new StringBuilder();

            public void Append(string data)
            {
                lock (_lock)
                {
                    _buffer.Append(data);
                }
            }

            public string GetResults()
            {
                lock (_lock)
                {
                    var data = _buffer.ToString();
                    _buffer.Clear();
                    return data;
                }
            }
        }
    }
}
