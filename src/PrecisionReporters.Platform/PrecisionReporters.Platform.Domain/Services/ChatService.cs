using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Twilio.Rest.Conversations.V1.Service;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class ChatService : IChatService
    {
        private readonly ITwilioService _twilioService;
        private readonly IDepositionRepository _depositionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserService _userService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ITwilioService twilioService,
            IDepositionRepository depositionRepository,
            IUserRepository userRepository,
            IUserService userService, ILogger<ChatService> logger)
        {
            _twilioService = twilioService;
            _depositionRepository = depositionRepository;
            _userRepository = userRepository;
            _userService = userService;
            _logger = logger;
        }

        private async Task<Result<string>> CreateChat(Guid depositionId)
        {
            var chatResult = await _twilioService.CreateChat(depositionId.ToString());
            if (chatResult.IsFailed)
            {
                _logger.LogInformation($"{nameof(DepositionService)}.{nameof(ChatService.CreateChat)} Error creating chat with name: {depositionId}");
                return Result.Fail($"Error creating chat with name: {depositionId}");
            }
            return chatResult;
        }

        public async Task<Result<JoinChatDto>> ManageChatParticipant(Guid depositionId)
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return Result.Fail("User not found");

            var includes = new[] { nameof(Deposition.Participants), $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" };
            var deposition = await _depositionRepository.GetFirstOrDefaultByFilter(x => x.Id == depositionId, includes);
            if (deposition == null)
            {
                _logger.LogInformation($"{nameof(DepositionService)}.{nameof(ChatService.CreateChat)} Deposition with ID: {depositionId} not found");
                return Result.Fail($"Deposition not found with ID: {depositionId}");
            }
            
            if (string.IsNullOrEmpty(deposition.ChatSid))
            {
                var createChatResult = await CreateChat(depositionId);
                if (createChatResult.IsFailed)
                    return createChatResult.ToResult();

                deposition.ChatSid = createChatResult.Value;
                await _depositionRepository.Update(deposition);
            }

            var participant = deposition.Participants.FirstOrDefault(x => x.UserId == user.Id);
            if (participant==null)
                return Result.Fail($"Participant not found with USER ID: {user.Id}");

            var twilioIdentity = new TwilioIdentity
            {
                Email = user.EmailAddress,
                FirstName = participant.Name ?? user.FirstName,
                LastName = participant.LastName ?? user.LastName,
                IsAdmin = user.IsAdmin ? 1 : 0,
                Role = (int)participant.Role
            };

            Result<UserResource> conversationUser = null;

            if (string.IsNullOrWhiteSpace(user.SId))
            {
                conversationUser = await _twilioService.CreateChatUser(twilioIdentity);
                if (conversationUser.IsFailed)
                    return conversationUser.ToResult();
                user.SId = conversationUser?.Value?.Sid;
                await _userRepository.Update(user);                
            }

            await _twilioService.AddUserToChat(deposition.ChatSid, twilioIdentity, user.SId);

            var token = _twilioService.GenerateToken(string.Empty, twilioIdentity, true);
            var result = new JoinChatDto { Token = token };
            return Result.Ok(result);
        }
    }
}