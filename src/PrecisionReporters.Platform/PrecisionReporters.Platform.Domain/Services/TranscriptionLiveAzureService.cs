using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
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
        private readonly ISignalRNotificationManager _signalRNotificationManager;
        private readonly IMapper<Transcription, TranscriptionDto, object> _transcriptionMapper;
        private readonly IUserRepository _userRepository;

        private Guid? _currentId;
        private bool _shouldClose = false;
        private static readonly SemaphoreSlim _shouldCloseSemaphore = new SemaphoreSlim(1);
        private bool _isClosed = true;
        private static readonly SemaphoreSlim _isClosedSemaphore = new SemaphoreSlim(1);
        private static readonly SemaphoreSlim _fluentTranscriptionSemaphore = new SemaphoreSlim(1);

        public TranscriptionLiveAzureService(IOptions<AzureCognitiveServiceConfiguration> azureConfiguration, ITranscriptionService transcriptionService,
            ILogger<TranscriptionLiveAzureService> logger, ISignalRNotificationManager signalRNotificationManager, IMapper<Transcription, TranscriptionDto, object> transcriptionMapper,
            IUserRepository userRepository)
        {
            _azureConfiguration = azureConfiguration.Value;
            _transcriptionService = transcriptionService;
            _logger = logger;
            _signalRNotificationManager = signalRNotificationManager;
            _transcriptionMapper = transcriptionMapper;
            _userRepository = userRepository;
        }

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

            _recognizer.Recognizing += async (s, e) =>
            {
                await _fluentTranscriptionSemaphore.WaitAsync();
                if (_currentId == null)
                    _currentId = Guid.NewGuid();

                _fluentTranscriptionSemaphore.Release();

                await HandleRecognizedSpeech(e);
            };

            _recognizer.Recognized += async (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    await HandleRecognizedSpeech(e, true);
                    await _fluentTranscriptionSemaphore.WaitAsync();
                    _currentId = null;
                    _fluentTranscriptionSemaphore.Release();
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

        private async Task HandleRecognizedSpeech(SpeechRecognitionEventArgs e, bool isFinalTranscript = false)
        {
            try
            {
                var bestTranscription = e.Result.Best().FirstOrDefault();
                var durationInMilliseconds = e.Result.Duration.Milliseconds;
                var offset = TimeSpan.FromTicks(e.Result.OffsetInTicks).TotalSeconds;
                var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == _userEmail);

                //OffSet
                var transcription = new Transcription
                {
                    Id = _currentId ?? Guid.NewGuid(),
                    TranscriptDateTime = DateTime.UtcNow,
                    Text = e.Result.Text,
                    Duration = durationInMilliseconds,
                    Confidence = bestTranscription != null ? bestTranscription.Confidence : 0.0,
                    DepositionId = new Guid(_depositionId),
                    User = user,
                    UserId = user.Id
                };

                var transcriptionDto = _transcriptionMapper.ToDto(transcription);
                
                var notificationtDto = new NotificationDto
                {
                    Action = NotificationAction.Create,
                    EntityType = NotificationEntity.Transcript,
                    Content = transcriptionDto
                };

                await _signalRNotificationManager.SendNotificationToDepositionMembers(transcriptionDto.DepositionId, notificationtDto);

                if (isFinalTranscript)
                    await _transcriptionService.StoreTranscription(transcription, _depositionId, _userEmail);
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
