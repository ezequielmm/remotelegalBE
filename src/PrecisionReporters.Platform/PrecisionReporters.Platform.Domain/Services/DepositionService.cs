using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Witness), nameof(Deposition.Case)};
            return await GetByIdWithIncludes(id, includes);
        }

        public async Task<Result<Deposition>> GetDepositionByIdWithDocumentUsers(Guid id)
        {
            var include = new[] { nameof(Deposition.DocumentUserDepositions) };
            return await GetByIdWithIncludes(id, include);
        }

        public async Task<Result<Deposition>> GenerateScheduledDeposition(Deposition deposition, List<Document> uploadedDocuments)
        {
            var requester = await _userService.GetUserByEmail(deposition.Requester.EmailAddress);
            if (requester.IsFailed)
            {
                return Result.Fail(new ResourceNotFoundError($"Requester with email {deposition.Requester.EmailAddress} not found"));
            }
            deposition.Requester = requester.Value;

            if (deposition.Witness != null && !string.IsNullOrWhiteSpace(deposition.Witness.Email))
            {
                var witnessUser = await _userService.GetUserByEmail(deposition.Witness.Email);
                if (witnessUser.IsSuccess)
                {
                    deposition.Witness.User = witnessUser.Value;
                }
            }

            if (deposition.Participants != null)
            {
                var participantUsers = await _userService.GetUsersByFilter(x => deposition.Participants.Select(p => p.Email).Contains(x.EmailAddress));
                foreach (var participant in deposition.Participants.Where(participant => !string.IsNullOrWhiteSpace(participant.Email)))
                {
                    participant.User = participantUsers.Find(x => x.EmailAddress == participant.Email);
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

        public async Task<List<Deposition>> GetDepositionsByStatus(DepositionStatus? status, DepositionSortField? sortedField,
            SortDirection? sortDirection, string userEmail)
        {
            
            var userResult = await _userService.GetUserByEmail(userEmail);

            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Witness), nameof(Deposition.Case)};

            Expression<Func<Deposition, bool>> filter = x => status != null ? x.Status == status : true;

            if (userResult.IsFailed || !userResult.Value.IsAdmin)
            {
                filter = x => (status != null ? x.Status == status : true) && (x.Participants.Any(p => p.Email == userEmail) || x.Requester.EmailAddress == userEmail);
            }

            Expression <Func<Deposition, object>> orderBy = sortedField switch
            {
                DepositionSortField.Details => x => x.Details,
                DepositionSortField.Status => x => x.Status,
                DepositionSortField.CaseNumber => x => x.Case.CaseNumber,
                DepositionSortField.CaseName => x => x.Case.Name,
                DepositionSortField.Company => x => x.Requester.CompanyName,
                DepositionSortField.Requester => x => x.Requester.LastName,
                _ => x => x.StartDate,
            };

            var depositions = await _depositionRepository.GetByStatus(
                orderBy,
                sortDirection ?? SortDirection.Ascend,
                filter,
                includes);

            return depositions;
        }

        public async Task<Result<JoinDepositionDto>> JoinDeposition(Guid id, string identity)
        {
            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Witness), nameof(Deposition.Room) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            if (deposition.Room.Status == RoomStatus.Created)
            {
                await _roomService.StartRoom(deposition.Room);
            }

            var token = await _roomService.GenerateRoomToken(deposition.Room.Name, identity);
            if (token.IsFailed)
                return token.ToResult<JoinDepositionDto>();

            var joinDepositionInfo = new JoinDepositionDto
            {
                WitnessEmail = deposition.Witness?.Email,
                Token = token.Value,
                TimeZone = deposition.TimeZone
            };

            return Result.Ok(joinDepositionInfo);
        }

        public async Task<Result<Deposition>> EndDeposition(Guid id)
        {
            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Room) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            var roomResult = await _roomService.EndRoom(deposition.Room);
            if (roomResult.IsFailed)
                return roomResult.ToResult<Deposition>();

            deposition.CompleteDate = DateTime.UtcNow;
            deposition.Status = DepositionStatus.Complete;

            var updatedDeposition = await _depositionRepository.Update(deposition);
            return Result.Ok(updatedDeposition);
        }
       
        public async Task<Result<Deposition>> AddDepositionEvent(Guid id, DepositionEvent depositionEvent, string userEmail)
        {
            var depositionResult = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Events) });
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
                return userResult.ToResult<Deposition>();

            depositionEvent.User = userResult.Value;
            depositionResult.Events.Add(depositionEvent);
            var updatedDeposition = await _depositionRepository.Update(depositionResult);
            return Result.Ok(updatedDeposition);
        }

        public async Task<Result<Deposition>> GoOnTheRecord(Guid id, bool onTheRecord, string userEmail)
        {
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Witness), nameof(Deposition.Case), nameof(Deposition.Events)};

            var depositionResult = await GetByIdWithIncludes(id, includes);
            if (depositionResult.IsFailed)
                return depositionResult;

            var deposition = depositionResult.Value;
            if (deposition.IsOnTheRecord == onTheRecord)
            {
                return Result.Fail(new InvalidInputError($"The current deposition is already in status onTheRecord: {onTheRecord}"));
            }

            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
                return userResult.ToResult<Deposition>();

            
            var depositionEvent = new DepositionEvent
            {
                EventType = onTheRecord ? EventType.OnTheRecord : EventType.OffTheRecord,
                User = userResult.Value
            };
            deposition.Events.Add(depositionEvent);
            deposition.IsOnTheRecord = onTheRecord;
            var updatedDeposition = await _depositionRepository.Update(deposition);
            return Result.Ok(updatedDeposition);
        }

        private async Task<Result<Deposition>> GetByIdWithIncludes(Guid id, string[] include = null)
        {
            var deposition = await _depositionRepository.GetById(id, include);
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            return Result.Ok(deposition);
        }
    }
}
