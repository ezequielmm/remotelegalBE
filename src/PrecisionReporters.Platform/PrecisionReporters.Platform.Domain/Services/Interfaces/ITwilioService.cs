﻿using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Entities;
using Twilio.Rest.Video.V1;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITwilioService
    {
        Task<RoomResource> CreateRoom(Room room);

        Task<RoomResource> GetRoom(string roomName);

        string GenerateToken(string roomName, string username);

        Task<RoomResource> EndRoom(string Sid);

        Task<CompositionResource> CreateComposition(RoomResource roomSid);

        Task<bool> GetCompositionMediaAsync(Composition composition);

        Task<bool> UploadCompositionMediaAsync(Composition composition);
    }
}
