using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TranscriptionLiveAzureService : ITranscriptionLiveService
    {
        private string _depositionId;
        private string _userEmail;
        private const int ChannelCount = 1;
        private const int BitsPerSample = 16;
        private readonly AzureCognitiveServiceConfiguration _azureConfiguration;
        private readonly ITranscriptionService _transcriptionService;
        private readonly ILogger<TranscriptionLiveAzureService> _logger;
        private PushAudioInputStream _audioInputStream;
        private SpeechRecognizer _recognizer;

        private bool _shouldClose = false;
        private static readonly SemaphoreSlim _shouldCloseSemaphore = new SemaphoreSlim(1);
        private bool _isClosed = true;
        private static readonly SemaphoreSlim _isClosedSemaphore = new SemaphoreSlim(1);

        public TranscriptionLiveAzureService(IOptions<AzureCognitiveServiceConfiguration> azureConfiguration, ITranscriptionService transcriptionService, ILogger<TranscriptionLiveAzureService> logger)
        {
            _azureConfiguration = azureConfiguration.Value;
            _transcriptionService = transcriptionService;
            _logger = logger;
        }

        public event TranscriptionAvailableEventHandler OnTranscriptionAvailable;

        public async Task RecognizeAsync(byte[] audioChunk)
        {
            await _isClosedSemaphore.WaitAsync();
            if (_isClosed)
            {
                // If recognition was stopped it needs to start again
                await _recognizer.StartContinuousRecognitionAsync()
                 .ConfigureAwait(false);
                _isClosed = false;
            }
            _isClosedSemaphore.Release();

            await _shouldCloseSemaphore.WaitAsync();
            if (_shouldClose)
            {
                // If recognition was getting closed, cancel the process once new audio arrives, which means the recognition should be resumed
                _shouldClose = false;
            }
            _shouldCloseSemaphore.Release();

            _audioInputStream.Write(audioChunk, audioChunk.Length);
        }

        public async Task InitializeRecognition(string userEmail, string depositionId, int sampleRate)
        {
            _userEmail = userEmail;
            _depositionId = depositionId;

            var speechConfig = SpeechConfig.FromSubscription(_azureConfiguration.SubscriptionKey, _azureConfiguration.RegionCode);

            _audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(Convert.ToUInt16(sampleRate), BitsPerSample, ChannelCount));
            var audioConfig = AudioConfig.FromStreamInput(_audioInputStream);

            _recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            _recognizer.Recognized += async (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    await HandleRecognizedSpeech(e);
                }

                await _shouldCloseSemaphore.WaitAsync();
                if (_shouldClose)
                {
                    await _recognizer.StopContinuousRecognitionAsync();
                    _shouldClose = false;
                }
                _shouldCloseSemaphore.Release();
            };

            _recognizer.SessionStopped += _recognizer_SessionStopped;

            await _recognizer.StartContinuousRecognitionAsync()
                             .ConfigureAwait(false);
            _isClosed = false;
        }

        private async void _recognizer_SessionStopped(object sender, SessionEventArgs e)
        {
            await _isClosedSemaphore.WaitAsync();
            _isClosed = true;
            _isClosedSemaphore.Release();
        }

        private async Task HandleRecognizedSpeech(SpeechRecognitionEventArgs e)
        {
            try
            {
                var transcription = new Transcription
                {
                    TranscriptDateTime = DateTime.UtcNow, // TODO: adjust based on duration and offset
                    Text = e.Result.Text
                };

                var transcriptionResult = await _transcriptionService.StoreTranscription(transcription, _depositionId, _userEmail);

                OnTranscriptionAvailable?.Invoke(this, new TranscriptionEventArgs { Transcription = transcriptionResult.Value });
            }
            catch (ObjectDisposedException ex)
            {
                // TODO: Transcriptions may arrive after the WS is closed so objects would be disposed
                _logger.LogError(ex, "Trying to process transcription when the websocket was already closed");
            }
        }
        public void StopTranscriptStream()
        {
            _shouldClose = true;

            var silenceBuffer = new byte[1024 * 512]; // 500kb
            _audioInputStream.Write(silenceBuffer, silenceBuffer.Length);
        }
    }
}
