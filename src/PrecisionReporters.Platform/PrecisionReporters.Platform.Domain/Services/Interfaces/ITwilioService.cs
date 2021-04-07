﻿using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System.Threading.Tasks;
using Twilio.Rest.Video.V1;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITwilioService
    {
        Task<Room> CreateRoom(Room room);
        Task<RoomResource> GetRoom(string roomName);
        string GenerateToken(string roomName, TwilioIdentity identity);
        Task<Result> EndRoom(Room room);
        Task<CompositionResource> CreateComposition(Room room, string witnessEmail);
        Task<bool> GetCompositionMediaAsync(Composition composition);
        Task<bool> UploadCompositionMediaAsync(Composition composition);
        Task<Result> UploadCompositionMetadata(CompositionRecordingMetadata metadata);
        Task<Result> DeleteCompositionAndRecordings(DeleteTwilioRecordingsDto deleteTwilioRecordings);
    }
}
