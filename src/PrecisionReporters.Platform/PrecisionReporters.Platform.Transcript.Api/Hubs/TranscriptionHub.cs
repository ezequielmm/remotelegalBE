﻿using FluentResults;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Authorization.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Extensions;
using PrecisionReporters.Platform.Transcript.Api.Hubs.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PrecisionReporters.Platform.Transcript.Api.Utils.Interfaces;

namespace PrecisionReporters.Platform.Transcript.Api.Hubs
{
    [Authorize]
    [HubName("/transcriptionHub")]
    public class TranscriptionHub : Hub<ITranscriptionClient>
    {
        private readonly ILogger<TranscriptionHub> _logger;
        private readonly ISignalRTranscriptionFactory _signalRTranscriptionFactory;

        public TranscriptionHub(ILogger<TranscriptionHub> logger,
            ISignalRTranscriptionFactory signalRTranscriptionFactory)
        {
            _logger = logger;
            _signalRTranscriptionFactory = signalRTranscriptionFactory;
        }

        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<Result> SubscribeToDeposition(SubscribeToDepositionDto dto)
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
        }

        public async Task UploadTranscription(TranscriptionsHubDto dto)
        {
            try
            {
                if (dto.Audio.Length == 0)
                {
                    _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);
                    return;
                }

                var transcriptionLiveService = _signalRTranscriptionFactory.GetTranscriptionLiveService(Context.ConnectionId);
                if (transcriptionLiveService == null)
                {
                    await _signalRTranscriptionFactory.TryInitializeRecognition(Context.ConnectionId, Context.UserIdentifier, dto.DepositionId, dto.SampleRate);
                    var currentTranscriptionLiveService = _signalRTranscriptionFactory.GetTranscriptionLiveService(Context.ConnectionId);
                    await currentTranscriptionLiveService.RecognizeAsync(dto.Audio);
                }
                else
                {
                    await transcriptionLiveService.RecognizeAsync(dto.Audio);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There was an error uploading transcription of connectionId {0} on Deposition {0}}", Context.ConnectionId, dto.DepositionId);
                throw;
            }
        }

        public async Task ChangeTranscriptionStatus(TranscriptionsChangeStatusDto dto)
        {
            var transcriptionHubQuery = ExtractTranscriptionHubQuery(Context.GetHttpContext());
            try
            {
                if (dto.OffRecord)
                {
                    _logger.LogInformation("Going OFF Record: transcriptions of {0} on deposition {1} with sample rate {2}", Context.UserIdentifier ,transcriptionHubQuery.DepositionId, transcriptionHubQuery.SampleRate);

                    _signalRTranscriptionFactory.Unsubscribe(Context.ConnectionId);
                }
                else
                {
                    await _signalRTranscriptionFactory.TryInitializeRecognition(Context.ConnectionId, Context.UserIdentifier, transcriptionHubQuery.DepositionId, transcriptionHubQuery.SampleRate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There was an error uploading transcription of connectionId {0} on Deposition {0}}", Context.ConnectionId, transcriptionHubQuery.DepositionId);
                throw;
            }
        }

        public override async Task OnConnectedAsync()
        {
            var transcriptionHubQuery = ExtractTranscriptionHubQuery(Context.GetHttpContext());
            _logger.LogInformation("Trying to initialize recognition on Connection ID {0}, from user: {1} of deposition: {2}", Context.ConnectionId, Context.UserIdentifier, transcriptionHubQuery.DepositionId);
            await _signalRTranscriptionFactory.TryInitializeRecognition(Context.ConnectionId, Context.UserIdentifier, transcriptionHubQuery.DepositionId, transcriptionHubQuery.SampleRate);
            await base.OnConnectedAsync();
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

        private TranscriptionHubQueryDto ExtractTranscriptionHubQuery(HttpContext httpContext)
        {
            var transcriptionHubQuery = new TranscriptionHubQueryDto
            {
                SampleRate = ApplicationConstants.DefaultSampleRate,
                DepositionId = httpContext.Request.Query["depositionId"].ToString()
            };
            if (int.TryParse(httpContext.Request.Query["sampleRate"], out var requestSampleRate))
            {
                transcriptionHubQuery.SampleRate = requestSampleRate;
            }

            return transcriptionHubQuery;
        }
    }
}