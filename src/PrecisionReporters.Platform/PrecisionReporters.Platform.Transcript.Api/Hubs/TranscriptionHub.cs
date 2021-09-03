﻿using FluentResults;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Authorization.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Shared.Commons;
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
                    _logger.LogError(ex, "There was an error subscribing to Deposition {0}", dto.DepositionId);
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
                    var transcriptionLiveService = _signalRTranscriptionFactory.GetTranscriptionLiveService(Context.ConnectionId);
                    if (transcriptionLiveService != null)
                        await transcriptionLiveService.RecognizeAsync(dto.Audio);
                    else
                    {
                        _logger.LogWarning("Reconnecting transcription service for user {0} with connectionID {1} on deposition {2}", Context.UserIdentifier,Context.ConnectionId,dto.DepositionId);
                        await _signalRTranscriptionFactory.TryInitializeRecognition(Context.ConnectionId, Context.UserIdentifier, dto.DepositionId.ToString(), dto.SampleRate);
                        transcriptionLiveService = _signalRTranscriptionFactory.GetTranscriptionLiveService(Context.ConnectionId);
                        if (transcriptionLiveService != null)
                            await transcriptionLiveService.RecognizeAsync(dto.Audio);
                        else
                        {
                            _logger.LogError("FAIL Reconnection transcription service for user {0} with connectionID {1} on deposition {2}", Context.UserIdentifier,Context.ConnectionId,dto.DepositionId);
                            return Result.Fail($"FAIL Reconnection transcription service for user {Context.UserIdentifier} with connectionID {Context.ConnectionId} on deposition {dto.DepositionId}");
                        }
                    }
                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "There was an error uploading transcription of connectionId {0} on Deposition {1}", Context.ConnectionId, dto.DepositionId);
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
                        _logger.LogInformation("Going OFF Record: transcriptions of {0} on deposition {1}", Context.UserIdentifier, dto.DepositionId);

                        _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);
                    }
                    else
                    {
                        _logger.LogInformation("Going ON Record: transcriptions of {0} on deposition {1}", Context.UserIdentifier, dto.DepositionId.ToString());
                        await _signalRTranscriptionFactory.TryInitializeRecognition(Context.ConnectionId, Context.UserIdentifier, dto.DepositionId.ToString(), dto.SampleRate);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "There was an error uploading transcription status of connectionId {0} on Deposition {1}", Context.ConnectionId, dto.DepositionId.ToString());
                    throw;
                }
            });
        }

        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task InitializeRecognition(InitializeRecognitionDto dto)
        {
            await _loggingHelper.ExecuteWithDeposition(dto.DepositionId, async () =>
            {
                var transcriptionLiveService = _signalRTranscriptionFactory.GetTranscriptionLiveService(Context.ConnectionId);
                if (transcriptionLiveService != null)
                {
                    _logger.LogInformation("Removing transcription service for user {0} on deposition {1}. Service already exist.", Context.UserIdentifier, dto.DepositionId);
                    _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);
                }
                _logger.LogInformation("Initializing transcription service for user {0} on deposition {1} with sample rate {2}", Context.UserIdentifier, dto.DepositionId, dto.SampleRate);
                await _signalRTranscriptionFactory.TryInitializeRecognition(Context.ConnectionId, Context.UserIdentifier, dto.DepositionId.ToString(), dto.SampleRate);
                return true;
            });
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
                _logger.LogError(exception, "Exception OnDisconnectedAsync {0} from user: {1}", Context.ConnectionId, Context.UserIdentifier);

            _logger.LogInformation("OnDisconnectedAsync {0} user: {1}", Context.ConnectionId, Context.UserIdentifier);
            try
            {
                _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "There was an error when unsubscribing user {0} with connection id {1}", Context.UserIdentifier, Context.ConnectionId, e.Message);
            }
            finally
            {
                await base.OnDisconnectedAsync(exception);
            }
        }
    }
}