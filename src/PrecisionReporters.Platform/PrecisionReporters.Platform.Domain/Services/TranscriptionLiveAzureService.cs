using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
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
        private PushAudioInputStream _audioInputStream;
        private SpeechRecognizer _recognizer;

        private bool _shouldClose = false;
        private static readonly SemaphoreSlim _shouldCloseSemaphore = new SemaphoreSlim(1);
        private bool _isClosed = true;
        private static readonly SemaphoreSlim _isClosedSemaphore = new SemaphoreSlim(1);

        public TranscriptionLiveAzureService(IOptions<AzureCognitiveServiceConfiguration> azureConfiguration, ITranscriptionService transcriptionService)
        {
            _azureConfiguration = azureConfiguration.Value;
            _transcriptionService = transcriptionService;
        }

        public event TranscriptionAvailableEventHandler OnTranscriptionAvailable;

        public async Task RecognizeAsync(byte[] audioChunk)
        {
            await _isClosedSemaphore.WaitAsync();
            if (_isClosed)
            {
                await _recognizer.StartContinuousRecognitionAsync()
                 .ConfigureAwait(false);
                _isClosed = false;
            }
            _isClosedSemaphore.Release();

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
            };

            await _recognizer.StartContinuousRecognitionAsync()
                             .ConfigureAwait(false);
            _isClosed = false;
        }

        private async Task HandleRecognizedSpeech(SpeechRecognitionEventArgs e)
        {
            var transcription = new Transcription
            {
                TranscriptDateTime = DateTime.UtcNow,
                Text = e.Result.Text
            };

            var transcriptionResult = await _transcriptionService.StoreTranscription(transcription, _depositionId, _userEmail);

            OnTranscriptionAvailable?.Invoke(this, new TranscriptionEventArgs { Transcription = transcriptionResult.Value });

            await _shouldCloseSemaphore.WaitAsync();
            await _isClosedSemaphore.WaitAsync();
            if (_shouldClose)
            {
                await _recognizer.StopContinuousRecognitionAsync();
                _shouldClose = false;
                _isClosed = true;
            }
            _isClosedSemaphore.Release();
            _shouldCloseSemaphore.Release();
        }

        public void StopTranscriptStream()
        {
            _shouldClose = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            SendFinalSilences(); // do not await on purpose
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task SendFinalSilences()
        {
            var silenceBuffer = new byte[1024 * 8 * 10];
            while (true)
            {
                await _isClosedSemaphore.WaitAsync();
                if (_isClosed)
                {
                    _isClosedSemaphore.Release();
                    break;
                }

                _isClosedSemaphore.Release();

                _audioInputStream.Write(silenceBuffer, silenceBuffer.Length);

                await Task.Delay(1000);
            }
        }
    }
}
