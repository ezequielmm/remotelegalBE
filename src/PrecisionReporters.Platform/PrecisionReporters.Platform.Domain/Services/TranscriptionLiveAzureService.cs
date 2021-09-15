using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Exceptions.CognitiveServices;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TranscriptionLiveAzureService : ITranscriptionLiveService
    {
        private readonly AzureCognitiveServiceConfiguration _azureConfiguration;
        private readonly IFireAndForgetService _fireAndForgetService;
        private readonly ILogger<TranscriptionLiveAzureService> _logger;
        private readonly ISignalRTranscriptionManager _signalRTranscriptionManager;
        private readonly IMapper<Transcription, TranscriptionDto, object> _transcriptionMapper;
        private readonly IUserRepository _userRepository;

        private string _instanceDepositionId;
        private string _instanceUserEmail;
        private User _instanceUser;
        private TranscriptionInfo _currentTranscriptionInfo = null;

        private PushAudioInputStream _audioInputStream;
        private SpeechRecognizer _recognizer;
        private bool _speechRecognizerSessionStopped = false;

        private bool _disposed = false;

        public TranscriptionLiveAzureService(IOptions<AzureCognitiveServiceConfiguration> azureConfiguration,
            ILogger<TranscriptionLiveAzureService> logger,
            ISignalRTranscriptionManager signalRTranscriptionManager,
            IMapper<Transcription, TranscriptionDto, object> transcriptionMapper,
            IUserRepository userRepository,
            IFireAndForgetService fireAndForgetService)
        {
            _azureConfiguration = azureConfiguration.Value;
            _logger = logger;
            _signalRTranscriptionManager = signalRTranscriptionManager;
            _transcriptionMapper = transcriptionMapper;
            _userRepository = userRepository;
            _fireAndForgetService = fireAndForgetService;
        }


        public async Task StartRecognitionAsync(string userEmail, string depositionId, int sampleRate)
        {
            _logger.LogDebug("Starting continuous recognition. Deposition: {DepositionId}. User: {UserEmail}. Sample rate: {SampleRate}.",
                depositionId, userEmail, sampleRate);

            await InitializeInstanceInformationAsync(userEmail, depositionId);

            const int bitsPerSample = 16;
            const int channelCount = 1;
            _audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(Convert.ToUInt16(sampleRate), bitsPerSample, channelCount));
            var audioConfig = AudioConfig.FromStreamInput(_audioInputStream);
            var speechConfig = SpeechConfig.FromSubscription(_azureConfiguration.SubscriptionKey, _azureConfiguration.RegionCode);
            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.RequestWordLevelTimestamps();
            speechConfig.OutputFormat = OutputFormat.Detailed;
            speechConfig.EnableDictation();

            _recognizer = new SpeechRecognizer(speechConfig, audioConfig);
            _recognizer.Recognizing += OnSpeechRecognizerRecognizing;
            _recognizer.Recognized += OnSpeechRecognizerRecognized;
            _recognizer.SessionStopped += OnSpeechRecognizerSessionStopped;
            _recognizer.Canceled += OnSpeechRecognizerCanceled;

            await _recognizer.StartContinuousRecognitionAsync();
        }

        public bool TryAddAudioChunkToBuffer(byte[] audioChunk)
        {
            if (_speechRecognizerSessionStopped)
            {
                return false;
            }

            _audioInputStream.Write(audioChunk, audioChunk.Length);
            return true;
        }

        public Task StopRecognitionAsync()
        {
            _logger.LogDebug("Stopping continuous recognition. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
            return _recognizer.StopContinuousRecognitionAsync();
        }

        private async Task InitializeInstanceInformationAsync(string userEmail, string depositionId)
        {
            _instanceUserEmail = userEmail;
            _instanceDepositionId = depositionId;
            _instanceUser = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == userEmail, tracking: false);
        }

        private async void OnSpeechRecognizerRecognizing(object sender, SpeechRecognitionEventArgs e)
        {
            var eventDateTime = DateTime.UtcNow;
            if (e.Result.Reason != ResultReason.RecognizingSpeech)
            {
                _logger.LogDebug("On SpeechRecognizer Recognized event had an invalid reason: {Reason}. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                    e.Result.Reason, _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
                return;
            }

            var newTranscription = _currentTranscriptionInfo == null;
            if (newTranscription)
            {
                _currentTranscriptionInfo = new TranscriptionInfo
                {
                    Id = Guid.NewGuid(),
                    FirstRecognizingAudioChunkDateTime = eventDateTime,
                };
            }

            var transcriptionInfo = _currentTranscriptionInfo.CreateCopy();
            await HandleRecognizingEventAsync(e, transcriptionInfo);
        }

        private async void OnSpeechRecognizerRecognized(object sender, SpeechRecognitionEventArgs e)
        {
            var eventDateTime = DateTime.UtcNow;
            if (e.Result.Reason != ResultReason.RecognizedSpeech)
            {
                _logger.LogDebug("On SpeechRecognizer Recognized event had an invalid reason: {Reason}. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                    e.Result.Reason, _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
                return;
            }

            var silenceRecognized = string.IsNullOrEmpty(e.Result.Text);
            if (silenceRecognized)
            {
                _logger.LogDebug("On SpeechRecognizer Recognized event recognized a silence. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                    e.Result.Reason, _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
                return;
            }

            var transcriptionInfo = _currentTranscriptionInfo.CreateCopy();
            _currentTranscriptionInfo = null;
            await HandleRecognizedEventAsync(e, eventDateTime, transcriptionInfo);
        }

        private void OnSpeechRecognizerSessionStopped(object sender, SessionEventArgs e)
        {
            _speechRecognizerSessionStopped = true;
            _logger.LogDebug("Speech recognition session stopped. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
        }

        private void OnSpeechRecognizerCanceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            var speechRecognitionResult = e.Result;
            var cancellationDetails = CancellationDetails.FromResult(speechRecognitionResult);
            _logger.LogWarning("Speech recognition canceled due to {Reason}. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                cancellationDetails.Reason, _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
            if (cancellationDetails.Reason == CancellationReason.Error)
            {
                _logger.LogError("Speech recognition canceled due to an error. (ErrorCode: {ErrorCode} | ErrorDetails: {ErrorDetails}). Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                    cancellationDetails.ErrorCode, cancellationDetails.ErrorDetails, _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
                ThrowExceptionIfAzureClosedConnectionDueToInactivity(cancellationDetails);
            }
        }

        private void ThrowExceptionIfAzureClosedConnectionDueToInactivity(CancellationDetails cancellationDetails)
        {
            if (cancellationDetails.ErrorCode != CancellationErrorCode.ServiceTimeout)
            {
                return;
            }

            const string AZURE_INACTIVITY_MESSAGE = "Due to service inactivity";
            if (!cancellationDetails.ErrorDetails.Contains(AZURE_INACTIVITY_MESSAGE, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            _speechRecognizerSessionStopped = true;
            throw new SpeechRecognizerInactivityException(cancellationDetails);
        }

        private Task HandleRecognizingEventAsync(SpeechRecognitionEventArgs e, TranscriptionInfo transcriptionInfo)
        {
            var transcription = BuildTranscription(e, transcriptionInfo);
            return SendNotificationToDepositionMembersAsync(transcription);
        }

        private Task HandleRecognizedEventAsync(SpeechRecognitionEventArgs e, DateTime eventDateTime, TranscriptionInfo transcriptionInfo)
        {
            var transcription = BuildTranscription(e, transcriptionInfo);
            var bestTranscription = e.Result.Best().OrderByDescending(x => x.Confidence).First();
            var latency = int.Parse(e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatencyMs, "0"));
            transcription.Confidence = bestTranscription.Confidence;
            transcription.TranscriptDateTime = eventDateTime.AddMilliseconds(-e.Result.Duration.TotalMilliseconds).AddMilliseconds(-latency);
            FireAndForgetStoreTranscription(transcription);
            return SendNotificationToDepositionMembersAsync(transcription);
        }

        private Transcription BuildTranscription(SpeechRecognitionEventArgs e, TranscriptionInfo transcriptionInfo)
        {
            var transcription = new Transcription
            {
                Id = transcriptionInfo.Id,
                TranscriptDateTime = transcriptionInfo.FirstRecognizingAudioChunkDateTime,
                Text = e.Result.Text,
                Duration = e.Result.Duration.Milliseconds,
                Confidence = 0,
                DepositionId = new Guid(_instanceDepositionId),
                UserId = _instanceUser.Id,
                User = _instanceUser,
                CreationDate = DateTime.UtcNow
            };
            return transcription;
        }

        private Task SendNotificationToDepositionMembersAsync(Transcription transcription)
        {
            var transcriptionDto = _transcriptionMapper.ToDto(transcription);
            var notificationDto = new NotificationDto
            {
                Action = NotificationAction.Create,
                EntityType = NotificationEntity.Transcript,
                Content = transcriptionDto
            };
            return _signalRTranscriptionManager.SendNotificationToDepositionMembers(transcriptionDto.DepositionId, notificationDto);
        }

        private void FireAndForgetStoreTranscription(Transcription transcription)
        {
            // Create a copy in order to avoid trying to persist the untracked User entity
            var transcriptionToStore = new Transcription();
            transcriptionToStore.CopyFrom(transcription);
            transcriptionToStore.TranscriptDateTime = transcriptionToStore.TranscriptDateTime.AddMilliseconds(-transcriptionToStore.TranscriptDateTime.Millisecond);

            _logger.LogDebug("Attempting to store transcription. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {TranscriptionId}.",
                transcription.DepositionId, _instanceUserEmail, transcription.Id);

            _fireAndForgetService.Execute<ITranscriptionService>(x =>
                x.StoreTranscription(transcriptionToStore, transcription.DepositionId.ToString(), _instanceUserEmail));
        }

        // Dispose Pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources
                _logger.LogDebug("Disposing live transcription service. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                    _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
                Task.Run(() =>
                {
                    try
                    {
                        // This takes too long so we are using fire and forget
                        _recognizer?.Dispose();
                        _audioInputStream?.Dispose();
                        _logger.LogDebug("Successfully dispos live transcription service. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {CurrentTranscriptionInfoId}.",
                            _instanceDepositionId, _instanceUserEmail, _currentTranscriptionInfo?.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An exception occurred while trying to dispose the {Service} as a fire and forget task.", nameof(ITranscriptionLiveService));
                    }
                });
            }

            // Dispose of unmanaged resources if any

            _disposed = true;
        }

        ~TranscriptionLiveAzureService()
        {
            Dispose(false);
        }

        private class TranscriptionInfo
        {
            public Guid Id { get; set; }
            public DateTime FirstRecognizingAudioChunkDateTime { get; set; }

            public TranscriptionInfo CreateCopy() => new TranscriptionInfo
            {
                Id = Id,
                FirstRecognizingAudioChunkDateTime = FirstRecognizingAudioChunkDateTime
            };
        }
    }
}
