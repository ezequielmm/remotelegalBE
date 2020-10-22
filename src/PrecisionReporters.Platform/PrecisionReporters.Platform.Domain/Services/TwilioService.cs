using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twilio;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Video.V1;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly TwilioAccountConfiguration _twilioAccountConfiguration;

        public TwilioService(IOptions<TwilioAccountConfiguration> twilioAccountConfiguration)
        {
            _twilioAccountConfiguration = twilioAccountConfiguration.Value ?? throw new ArgumentException(nameof(twilioAccountConfiguration));
            TwilioClient.Init(_twilioAccountConfiguration.AccountSid, _twilioAccountConfiguration.AuthToken);
        }

        public async Task<RoomResource> CreateRoom(string roomName)
        {
            var roomResource = await RoomResource.CreateAsync(uniqueName: roomName);
            return roomResource;
        }

        public async Task<RoomResource> GetRoom(string name)
        {
            var roomResource = await RoomResource.FetchAsync(pathSid: name);
            return roomResource;
        }

        public string GenerateToken(string roomName, string username)
        {
            var grant = new VideoGrant();
            grant.Room = roomName;
            var grants = new HashSet<IGrant> { grant };
            var token = new Token(_twilioAccountConfiguration.AccountSid, _twilioAccountConfiguration.ApiKeySid,
                _twilioAccountConfiguration.ApiKeySecret, identity: username.ToString(), grants: grants);

            return token.ToJwt();
        }
    }
}
