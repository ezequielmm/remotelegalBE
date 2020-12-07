using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DepositionService : IDepositionService
    {
        private readonly IDepositionRepository _depositionRepository;
        private readonly IUserService _userService;
        private readonly IRoomService _roomService;

        public DepositionService(IDepositionRepository depositionRepository, IUserService userService, IRoomService roomService)
        {
            _depositionRepository = depositionRepository;
            _userService = userService;
            _roomService = roomService;
        }

        public async Task<List<Deposition>> GetDepositions(Expression<Func<Deposition, bool>> filter = null,
            string[] include = null)
        {
            return await _depositionRepository.GetByFilter(filter, include);
        }

        public async Task<Result<Deposition>> GetDepositionById(Guid id)
        {
            var includes = new[] { nameof(Deposition.Witness), nameof(Deposition.Room) };
            var deposition = await _depositionRepository.GetById(id, includes);
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            return Result.Ok(deposition);
        }

        public async Task<Result<Deposition>> GenerateScheduledDeposition(Deposition deposition, List<DepositionDocument> uploadedDocuments)
        {
            var requester = await _userService.GetUserByEmail(deposition.Requester.EmailAddress);
            if (requester.IsFailed)
            {
                return Result.Fail(new ResourceNotFoundError($"Requester with email {deposition.Requester.EmailAddress} not found"));
            }
            deposition.Requester = requester.Value;

            if (deposition.Witness != null)
            {
                if (!string.IsNullOrWhiteSpace(deposition.Witness.Email))
                {
                    var witnessUser = await _userService.GetUserByEmail(deposition.Witness.Email);
                    if (witnessUser.IsSuccess)
                    {
                        deposition.Witness.User = witnessUser.Value;
                    }
                }
            }

            // If caption has a FileKey, find the matching document. If it doesn't has a FileKey, remove caption
            deposition.Caption = !string.IsNullOrWhiteSpace(deposition.FileKey) ? uploadedDocuments.First(d => d.FileKey == deposition.FileKey) : null;

            deposition.Room = new Room
            {
                Name = Guid.NewGuid().ToString(),
                CreationDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                IsRecordingEnabled = deposition.IsVideoRecordingNeeded
            };

            return Result.Ok(deposition);
        }

        public async Task<Result<JoinDepositionDto>> JoinDeposition(Guid id, string identity)
        {
            
            var depositionResult = await GetDepositionById(id);
            if (depositionResult.IsFailed)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            if (depositionResult.Value.Room.Status == RoomStatus.Created)
            {
                await _roomService.StartRoom(depositionResult.Value.Room);
            }

            var token = await _roomService.GenerateRoomToken(depositionResult.Value.Room.Name, identity);
            if (token.IsFailed)
                return token.ToResult<JoinDepositionDto>();

            var joinDepositionInfo = new JoinDepositionDto
            {
                WitnessEmail = depositionResult.Value.Witness?.Email,
                Token = token.Value
            };

            return Result.Ok(joinDepositionInfo);
        }
    }
}