using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Exceptions.CognitiveServices;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    // Disable rule: It's wrong to use a finalizer without having unmanaged resources to clean. See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#implement-the-dispose-pattern
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class TranscriptionLiveAzureService : ITranscriptionLiveService
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private readonly AzureCognitiveServiceConfiguration _azureConfiguration;
        private readonly IFireAndForgetService _fireAndForgetService;
        private readonly ILogger<TranscriptionLiveAzureService> _logger;
        private readonly ISignalRTranscriptionManager _signalRTranscriptionManager;
        private readonly IMapper<Transcription, TranscriptionDto, object> _transcriptionMapper;
        private readonly IUserRepository _userRepository;
        private readonly IParticipantRepository _participantRepository;

        private Guid _instanceDepositionId;
        private string _instanceUserEmail;
        private User _instanceUser;
        private Participant _instanceParticipant;

        private AudioStreamFormat _audioStreamFormat;
        private PushAudioInputStream _audioInputStream;
        private AudioConfig _audioConfig;
        private SpeechRecognizer _recognizer;
        private bool _continuousRecognitionStopped = false;

        private bool _disposed = false;

        public TranscriptionLiveAzureService(IOptions<AzureCognitiveServiceConfiguration> azureConfiguration,
            ILogger<TranscriptionLiveAzureService> logger,
            ISignalRTranscriptionManager signalRTranscriptionManager,
            IMapper<Transcription, TranscriptionDto, object> transcriptionMapper,
            IUserRepository userRepository,
            IFireAndForgetService fireAndForgetService,
            IParticipantRepository participantRepository)
        {
            _azureConfiguration = azureConfiguration.Value;
            _logger = logger;
            _signalRTranscriptionManager = signalRTranscriptionManager;
            _transcriptionMapper = transcriptionMapper;
            _userRepository = userRepository;
            _fireAndForgetService = fireAndForgetService;
            _participantRepository = participantRepository;
        }

        public async Task StartRecognitionAsync(string userEmail, string depositionId, int sampleRate)
        {
            _logger.LogInformation("Starting continuous recognition. Deposition: {DepositionId}. User: {UserEmail}. Sample rate: {SampleRate}.",
                depositionId, userEmail, sampleRate);

            await InitializeInstanceInformationAsync(userEmail, depositionId);

            const int bitsPerSample = 16;
            const int channelCount = 1;
            _audioStreamFormat = AudioStreamFormat.GetWaveFormatPCM(Convert.ToUInt16(sampleRate), bitsPerSample, channelCount);
            _audioInputStream = AudioInputStream.CreatePushStream(_audioStreamFormat);
            _audioConfig = AudioConfig.FromStreamInput(_audioInputStream);
            var speechConfig = SpeechConfig.FromSubscription(_azureConfiguration.SubscriptionKey, _azureConfiguration.RegionCode);
            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.RequestWordLevelTimestamps();
            speechConfig.OutputFormat = OutputFormat.Detailed;
            speechConfig.EnableDictation();

            _recognizer = new SpeechRecognizer(speechConfig, _audioConfig);
            _recognizer.Recognizing += OnSpeechRecognizerRecognizing;
            _recognizer.Recognized += OnSpeechRecognizerRecognized;
            _recognizer.SessionStopped += OnSpeechRecognizerSessionStopped;
            _recognizer.Canceled += OnSpeechRecognizerCanceled;

            await _recognizer.StartContinuousRecognitionAsync();
        }

        public bool TryAddAudioChunkToBuffer(byte[] audioChunk)
        {
            if (_continuousRecognitionStopped)
            {
                return false;
            }

            _audioInputStream.Write(audioChunk, audioChunk.Length);
            return true;
        }

        public Task StopRecognitionAsync()
        {
            _continuousRecognitionStopped = true;
            _logger.LogInformation("Stopping continuous recognition. Deposition: {DepositionId}. User: {UserEmail}.",
                _instanceDepositionId, _instanceUserEmail);
            return _recognizer.StopContinuousRecognitionAsync();
        }

        private async Task InitializeInstanceInformationAsync(string userEmail, string depositionId)
        {
            _instanceUserEmail = userEmail;
            _instanceDepositionId = Guid.Parse(depositionId);
            _instanceUser = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == userEmail, tracking: false);
            _instanceParticipant = await _participantRepository.GetFirstOrDefaultByFilter(p => p.UserId == _instanceUser.Id && p.DepositionId == _instanceDepositionId, tracking: false);
        }

        private async void OnSpeechRecognizerRecognizing(object sender, SpeechRecognitionEventArgs e)
        {
            var eventDateTime = DateTime.UtcNow;
            if (e.Result.Reason != ResultReason.RecognizingSpeech)
            {
                _logger.LogInformation("On SpeechRecognizer Recognized event had an invalid reason: {Reason}. Deposition: {DepositionId}. User: {UserEmail}.",
                    e.Result.Reason, _instanceDepositionId, _instanceUserEmail);
                return;
            }

            await HandleRecognizingEventAsync(e, eventDateTime);
        }

        private async void OnSpeechRecognizerRecognized(object sender, SpeechRecognitionEventArgs e)
        {
            var eventDateTime = DateTime.UtcNow;
            if (e.Result.Reason != ResultReason.RecognizedSpeech)
            {
                _logger.LogInformation("On SpeechRecognizer Recognized event had an invalid reason: {Reason}. Deposition: {DepositionId}. User: {UserEmail}.",
                    e.Result.Reason, _instanceDepositionId, _instanceUserEmail);
                return;
            }

            var silenceRecognized = string.IsNullOrEmpty(e.Result.Text);
            if (silenceRecognized)
            {
                _logger.LogInformation("On SpeechRecognizer Recognized event recognized a silence. Deposition: {DepositionId}. User: {UserEmail}.",
                    _instanceDepositionId, _instanceUserEmail);
                return;
            }

            await HandleRecognizedEventAsync(e, eventDateTime);
        }

        private void OnSpeechRecognizerSessionStopped(object sender, SessionEventArgs e)
        {
            _continuousRecognitionStopped = true;
            _logger.LogInformation("Speech recognition session stopped. Deposition: {DepositionId}. User: {UserEmail}.",
                _instanceDepositionId, _instanceUserEmail);
        }

        private void OnSpeechRecognizerCanceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            var speechRecognitionResult = e.Result;
            var cancellationDetails = CancellationDetails.FromResult(speechRecognitionResult);
            _logger.LogInformation("Speech recognition canceled due to {Reason}. Deposition: {DepositionId}. User: {UserEmail}.",
                cancellationDetails.Reason, _instanceDepositionId, _instanceUserEmail);
            if (cancellationDetails.Reason == CancellationReason.Error)
            {
                var logLevel = _continuousRecognitionStopped
                    ? LogLevel.Warning
                    : LogLevel.Error;
                _logger.Log(logLevel, "Speech recognition canceled due to an error. (ErrorCode: {ErrorCode} | ErrorDetails: {ErrorDetails}). Deposition: {DepositionId}. User: {UserEmail}.",
                    cancellationDetails.ErrorCode, cancellationDetails.ErrorDetails, _instanceDepositionId, _instanceUserEmail);
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

            _continuousRecognitionStopped = true;
            throw new SpeechRecognizerInactivityException(cancellationDetails);
        }

        private Task HandleRecognizingEventAsync(SpeechRecognitionEventArgs e, DateTime eventDateTime)
        {
            var transcription = BuildTranscription(e, eventDateTime);
            return SendRecognizingTranscriptionToDepositionMembersAsync(transcription);
        }

        private Task HandleRecognizedEventAsync(SpeechRecognitionEventArgs e, DateTime eventDateTime)
        {
            var transcription = BuildTranscription(e, eventDateTime);
            var bestTranscription = e.Result.Best().OrderByDescending(x => x.Confidence).First();
            transcription.Confidence = bestTranscription.Confidence;
            var latency = int.Parse(e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatencyMs, "0"));
            transcription.TranscriptDateTime = transcription.TranscriptDateTime.AddMilliseconds(-latency);
            FireAndForgetStoreTranscription(transcription);
            return SendRecognizedTranscriptionToDepositionMembersAsync(transcription);
        }

        private Transcription BuildTranscription(SpeechRecognitionEventArgs e, DateTime eventDateTime)
        {
            var transcription = new Transcription
            {
                Id = Guid.NewGuid(),
                TranscriptDateTime = eventDateTime.AddMilliseconds(-e.Result.Duration.TotalMilliseconds),
                Text = e.Result.Text,
                Duration = Convert.ToInt32(e.Result.Duration.TotalMilliseconds),
                Confidence = 0,
                DepositionId = _instanceDepositionId,
                UserId = _instanceUser.Id,
                User = _instanceUser,
                CreationDate = DateTime.UtcNow,
                ParticipantAlias = _instanceParticipant.GetFullName()
            };
            return transcription;
        }

        private Task SendRecognizingTranscriptionToDepositionMembersAsync(Transcription transcription)
        {
            var transcriptionDto = _transcriptionMapper.ToDto(transcription);
            transcriptionDto.Status = TranscriptionStatus.Recognizing;
            return SendNotificationToDepositionMembersAsync(transcriptionDto);
        }

        private Task SendRecognizedTranscriptionToDepositionMembersAsync(Transcription transcription)
        {
            var transcriptionDto = _transcriptionMapper.ToDto(transcription);
            transcriptionDto.Status = TranscriptionStatus.Recognized;
            return SendNotificationToDepositionMembersAsync(transcriptionDto);
        }

        private Task SendNotificationToDepositionMembersAsync(TranscriptionDto transcriptionDto)
        {
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

            _logger.LogInformation("Attempting to store transcription. Deposition: {DepositionId}. User: {UserEmail}. Transcription: {TranscriptionId}.",
                transcription.DepositionId, _instanceUserEmail, transcription.Id);

            _fireAndForgetService.Execute<ITranscriptionService>(x =>
                x.StoreTranscription(transcriptionToStore, transcription.DepositionId.ToString(), _instanceUserEmail));
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            _logger.LogInformation($"Disposing {nameof(TranscriptionLiveAzureService)}. Deposition: {{DepositionId}}. User: {{UserEmail}}.", _instanceDepositionId, _instanceUserEmail);

            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _audioStreamFormat?.Dispose();
                _audioInputStream?.Dispose();
                _audioConfig?.Dispose();
                _recognizer?.Dispose();
            }

            _disposed = true;
            _logger.LogInformation($"Successfully disposed {nameof(TranscriptionLiveAzureService)}. Deposition: {{DepositionId}}. User: {{UserEmail}}.",
                _instanceDepositionId, _instanceUserEmail);
        }
    }
}
