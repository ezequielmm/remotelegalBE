using FluentResults;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Shared.Authorization.Attributes;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Extensions;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using PrecisionReporters.Platform.Transcript.Api.Hubs.Interfaces;
using PrecisionReporters.Platform.Transcript.Api.Utils.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Hubs
{
    [Authorize]
    [HubName("/transcriptionHub")]
    public class TranscriptionHub : Hub<ITranscriptionClient>
    {
        private readonly ILogger<TranscriptionHub> _logger;
        private readonly ISignalRTranscriptionFactory _signalRTranscriptionFactory;
        private readonly ILoggingHelper _loggingHelper;

        public TranscriptionHub(ILogger<TranscriptionHub> logger,
            ISignalRTranscriptionFactory signalRTranscriptionFactory,
            ILoggingHelper loggingHelper)
        {
            _logger = logger;
            _signalRTranscriptionFactory = signalRTranscriptionFactory;
            _loggingHelper = loggingHelper;
        }

        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<Result> SubscribeToDeposition(SubscribeToDepositionDto dto)
        {
            return await _loggingHelper.ExecuteWithDeposition(dto.DepositionId, async () =>
            {
                try
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, dto.DepositionId.GetDepositionSignalRGroupName());
                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "There was an error subscribing to Deposition {DepositionId}", dto.DepositionId);
                    return Result.Fail($"Unable to add user to Group {ApplicationConstants.DepositionGroupName}{dto.DepositionId}.");
                }
            });
        }

        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task UploadTranscription(TranscriptionsHubDto dto)
        {
            await _loggingHelper.ExecuteWithDeposition(dto.DepositionId, async () =>
            {
                try
                {
                    var recognitionAlreadyInitialized = _signalRTranscriptionFactory.TryGetTranscriptionLiveService(Context.ConnectionId, out var transcriptionLiveService);
                    if (!recognitionAlreadyInitialized)
                    {
                        _logger.LogWarning("Reconnecting transcription service for user {UserIdentifier} with connectionID {ConnectionId} on deposition {DepositionId}", Context.UserIdentifier, Context.ConnectionId, dto.DepositionId);
                        await _signalRTranscriptionFactory.InitializeRecognitionAsync(Context.ConnectionId, Context.UserIdentifier, dto.DepositionId.ToString(), dto.SampleRate);
                        recognitionAlreadyInitialized = _signalRTranscriptionFactory.TryGetTranscriptionLiveService(Context.ConnectionId, out transcriptionLiveService);
                        if (!recognitionAlreadyInitialized)
                        {
                            _logger.LogError("FAIL Reconnection transcription service for user {UserIdentifier} with connectionID {ConnectionId} on deposition {DepositionId}", Context.UserIdentifier, Context.ConnectionId, dto.DepositionId);
                            return Result.Fail($"FAIL Reconnection transcription service for user {Context.UserIdentifier} with connectionID {Context.ConnectionId} on deposition {dto.DepositionId}");
                        }
                    }

                    if (!transcriptionLiveService.TryAddAudioChunkToBuffer(dto.Audio))
                    {
                        return await RenewTranscriptionServiceAndTryAddAudioChunkToBufferAsync(dto);
                    }

                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "There was an error uploading transcription of connectionId {ConnectionId} on Deposition {DepositionId}", Context.ConnectionId, dto.DepositionId);
                    throw;
                }
            });
        }

        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task ChangeTranscriptionStatus(TranscriptionsChangeStatusDto dto)
        {
            await _loggingHelper.ExecuteWithDeposition(dto.DepositionId, async () =>
            {
                try
                {
                    if (dto.OffRecord)
                    {
                        _logger.LogInformation("Going OFF Record: transcriptions of {UserIdentifier} on deposition {DepositionId}", Context.UserIdentifier, dto.DepositionId);
                        _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);
                    }
                    else
                    {
                        _logger.LogInformation("Going ON Record: transcriptions of {UserIdentifier} on deposition {DepositionId}", Context.UserIdentifier, dto.DepositionId);
                        await _signalRTranscriptionFactory.InitializeRecognitionAsync(Context.ConnectionId, Context.UserIdentifier, dto.DepositionId.ToString(), dto.SampleRate);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "There was an error uploading transcription status of connectionId {ConnectionId} on Deposition {DepositionId}", Context.ConnectionId, dto.DepositionId);
                    throw;
                }
            });
        }

        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task InitializeRecognition(InitializeRecognitionDto dto)
        {
            await _loggingHelper.ExecuteWithDeposition(dto.DepositionId, async () =>
            {
                var recognitionAlreadyInitialized = _signalRTranscriptionFactory.TryGetTranscriptionLiveService(Context.ConnectionId, out var _);
                if (recognitionAlreadyInitialized)
                {
                    _logger.LogInformation("Removing transcription service for user {UserIdentifier} on deposition {DepositionId}. Service already exist.", Context.UserIdentifier, dto.DepositionId);
                    _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);
                }
                _logger.LogInformation("Initializing transcription service for user {UserIdentifier} on deposition {DepositionId} with sample rate {SampleRate}", Context.UserIdentifier, dto.DepositionId, dto.SampleRate);
                await _signalRTranscriptionFactory.InitializeRecognitionAsync(Context.ConnectionId, Context.UserIdentifier, dto.DepositionId.ToString(), dto.SampleRate);
                return true;
            });
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
                _logger.LogError(exception, "Exception OnDisconnectedAsync {ConnectionId} from user: {UserIdentifier}", Context.ConnectionId, Context.UserIdentifier);

            _logger.LogInformation("OnDisconnectedAsync {ConnectionId} user: {UserIdentifier}", Context.ConnectionId, Context.UserIdentifier);
            try
            {
                _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "There was an error when unsubscribing user {UserIdentifier} with connection id {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            }
            finally
            {
                await base.OnDisconnectedAsync(exception);
            }
        }

        private async Task<Result> RenewTranscriptionServiceAndTryAddAudioChunkToBufferAsync(TranscriptionsHubDto dto)
        {
            _logger.LogWarning("Unable to send add audio chunk to buffer. Renewing transcription service for user {UserIdentifier} with connectionId {ConnectionId} on deposition {DepositionId}.", Context.UserIdentifier, Context.ConnectionId, dto.DepositionId);
            _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);
            await _signalRTranscriptionFactory.InitializeRecognitionAsync(Context.ConnectionId, Context.UserIdentifier, dto.DepositionId.ToString(), dto.SampleRate);
            var recognitionAlreadyInitialized = _signalRTranscriptionFactory.TryGetTranscriptionLiveService(Context.ConnectionId, out var transcriptionLiveService);
            if (!recognitionAlreadyInitialized)
            {
                _logger.LogError("Unable to retrieve transcription service for user {UserIdentifier} with connectionId {ConnectionId} on deposition {DepositionId}.", Context.UserIdentifier, Context.ConnectionId, dto.DepositionId);
                return Result.Fail($"FAIL Reconnection transcription service for user {Context.UserIdentifier} with connectionID {Context.ConnectionId} on deposition {dto.DepositionId}.");
            }

            if (!transcriptionLiveService.TryAddAudioChunkToBuffer(dto.Audio))
            {
                _logger.LogError("Unable to send add audio chunk to buffer after two attempts. Renewing transcription service for user {UserIdentifier} with connectionId {ConnectionId} on deposition {DepositionId}.", Context.UserIdentifier, Context.ConnectionId, dto.DepositionId);
                return Result.Fail($"FAIL Reconnection transcription service for user {Context.UserIdentifier} with connectionID {Context.ConnectionId} on deposition {dto.DepositionId}.");
            }

            return Result.Ok();
        }
    }
}