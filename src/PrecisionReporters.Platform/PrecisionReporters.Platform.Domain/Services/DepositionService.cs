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
        private const int DEFAULT_BREAK_ROOMS_AMOUNT = 4;
        private const string BREAK_ROOM_PREFIX = "BREAK_ROOM";
        private readonly IDepositionRepository _depositionRepository;
        private readonly IParticipantRepository _participantRepository;
        private readonly IDepositionEventRepository _depositionEventRepository;
        private readonly IUserService _userService;
        private readonly IRoomService _roomService;
        private readonly IBreakRoomService _breakRoomService;
        private readonly IPermissionService _permissionService;

        public DepositionService(IDepositionRepository depositionRepository,
            IParticipantRepository participantRepository,
			IDepositionEventRepository depositionEventRepository,
            IUserService userService,
            IRoomService roomService,
            IBreakRoomService breakRoomService,
            IPermissionService permissionService)
        {
            _depositionRepository = depositionRepository;
            _participantRepository = participantRepository;
            _depositionEventRepository = depositionEventRepository;
            _userService = userService;
            _roomService = roomService;
            _breakRoomService = breakRoomService;
            _permissionService = permissionService;
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

        public async Task<Result<Deposition>> GenerateScheduledDeposition(Guid caseId, Deposition deposition, List<Document> uploadedDocuments, User addedBy)
        {
            deposition.Id = Guid.NewGuid();
            var requesterResult = await _userService.GetUserByEmail(deposition.Requester.EmailAddress);
            if (requesterResult.IsFailed)
            {
                return Result.Fail(new ResourceNotFoundError($"Requester with email {deposition.Requester.EmailAddress} not found"));
            }
            deposition.Requester = requesterResult.Value;
            //Adding Requester as a Participant
            if (deposition.Participants == null)
                deposition.Participants = new List<Participant>();

            deposition.Participants.Add(new Participant(deposition.Requester, ParticipantType.Observer));

            deposition.AddedBy = addedBy;

            if (deposition.Witness != null && !string.IsNullOrWhiteSpace(deposition.Witness.Email))
            {
                var witnessUser = await _userService.GetUserByEmail(deposition.Witness.Email);
                if (witnessUser.IsSuccess)
                {
                    deposition.Witness.User = witnessUser.Value;
                }
            }

            // If caption has a FileKey, find the matching document. If it doesn't has a FileKey, remove caption
            deposition.Caption = !string.IsNullOrWhiteSpace(deposition.FileKey) ? uploadedDocuments.First(d => d.FileKey == deposition.FileKey) : null;

            deposition.Room = new Room(Guid.NewGuid().ToString(), deposition.IsVideoRecordingNeeded);

            deposition.CaseId = caseId;
            await AddParticipants(deposition);
            AddBreakRooms(deposition);
            await _depositionRepository.Create(deposition);

            return Result.Ok(deposition);
        }

        private async Task AddParticipants(Deposition deposition)
        {
            if (deposition.Participants != null)
            {
                var participantUsers = await _userService.GetUsersByFilter(x => deposition.Participants.Select(p => p.Email).Contains(x.EmailAddress));
                foreach (var participant in deposition.Participants.Where(participant => !string.IsNullOrWhiteSpace(participant.Email)))
                {
                    var user = participantUsers.Find(x => x.EmailAddress == participant.Email);
                    if (user != null)
                    {
                        participant.User = user;
                        await _permissionService.AddUserRole(participant.User.Id, deposition.Id, ResourceType.Deposition, ParticipantType.CourtReporter == participant.Role ? RoleName.DepositionCourtReporter : RoleName.DepositionAttendee);
                    }
                }
            }

            if (deposition.Witness?.User != null)
            {
                await _permissionService.AddUserRole(deposition.Witness.User.Id, deposition.Id, ResourceType.Deposition, RoleName.DepositionAttendee);
            }
        }

        private void AddBreakRooms(Deposition deposition)
        {
            if (!deposition.BreakRooms.Any())
            {
                for (int i = 1; i < (DEFAULT_BREAK_ROOMS_AMOUNT + 1); i++)
                {
                    deposition.BreakRooms.Add(new BreakRoom
                    {
                        Name = $"Breakroom {i}",
                        Room = new Room($"{deposition.Id}_{BREAK_ROOM_PREFIX}_{i}")
                    });
                }
            }
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
                filter = x => (status != null ? x.Status == status : true) &&
                    (x.Participants.Any(p => p.Email == userEmail)
                        || x.Requester.EmailAddress == userEmail
                        || x.AddedBy.EmailAddress == userEmail
                        || (x.Witness != null && x.Witness.Email == userEmail));
            }

            Expression<Func<Deposition, object>> orderBy = sortedField switch
            {
                DepositionSortField.Details => x => x.Details,
                DepositionSortField.Status => x => x.Status,
                DepositionSortField.CaseNumber => x => x.Case.CaseNumber,
                DepositionSortField.CaseName => x => x.Case.Name,
                DepositionSortField.Company => x => x.Requester.CompanyName,
                DepositionSortField.Requester => x => x.Requester.FirstName + x.Requester.LastName,
                _ => x => x.StartDate,
            };

            Expression<Func<Deposition, object>> orderByThen = x => x.Requester.LastName;

            var depositions = await _depositionRepository.GetByStatus(
                orderBy,
                sortDirection ?? SortDirection.Ascend,
                filter,
                includes,
                sortedField == DepositionSortField.Requester ? orderByThen : null
                );

            return depositions;
        }

        public async Task<Result<JoinDepositionDto>> JoinDeposition(Guid id, string identity)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult<JoinDepositionDto>();

            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Witness), nameof(Deposition.Room), nameof(Deposition.Participants) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            // TODO: Add distributed lock when our infra allows it
            if (deposition.Room.Status == RoomStatus.Created)
            {
                await _roomService.StartRoom(deposition.Room);
            }

            // TODO: Witness shoudl be part of the participants instead of a separated property.
            var currentParticipant = deposition.Witness?.Email == identity ? deposition.Witness : deposition.Participants.FirstOrDefault(p => p.User == userResult.Value);
            if (currentParticipant == null && !userResult.Value.IsAdmin)
                return Result.Fail(new InvalidInputError($"User is neither a Participant for this Deposition nor an Admin"));

            var role = currentParticipant?.Role ?? ParticipantType.Admin;

            var token = await _roomService.GenerateRoomToken(deposition.Room.Name, userResult.Value, role, identity);
            if (token.IsFailed)
                return token.ToResult<JoinDepositionDto>();

            var joinDepositionInfo = new JoinDepositionDto
            {
                Token = token.Value,
                TimeZone = deposition.TimeZone,
                IsOnTheRecord = deposition.IsOnTheRecord,
                IsSharing = deposition.SharingDocumentId.HasValue
            };

            return Result.Ok(joinDepositionInfo);
        }

        public async Task<Result<Deposition>> EndDeposition(Guid id)
        {
            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Room),
                $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}",
                nameof(Deposition.Witness) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            var roomResult = await _roomService.EndRoom(deposition.Room, deposition.Witness.Email);
            if (roomResult.IsFailed)
                return roomResult.ToResult<Deposition>();

            deposition.CompleteDate = DateTime.UtcNow;
            deposition.Status = DepositionStatus.Completed;

            var user = await _userService.GetCurrentUserAsync();
            await GoOnTheRecord(id, false, user.EmailAddress);

            var updatedDeposition = await _depositionRepository.Update(deposition);

            await _userService.RemoveGuestParticipants(deposition.Participants);

            return Result.Ok(updatedDeposition);
        }

        public async Task<Result<Participant>> GetDepositionParticipantByEmail(Guid id, string participantEmail)
        {
            var depositionResult = await GetDepositionById(id);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<Participant>();

            var participant = GetParticipantByEmail(depositionResult.Value, participantEmail);

            if (participant == null)
                return Result.Fail(new ResourceNotFoundError($"Participant with email {participantEmail} not found"));

            var userResult = await _userService.GetUserByEmail(participantEmail);
            if (!userResult.IsFailed)
                participant.User = userResult.Value;

            return Result.Ok(participant);
        }

        public async Task<Result<List<DepositionEvent>>> GetDepositionEvents(Guid id)
        {
            var depositionEvents = await _depositionEventRepository.GetByFilter(
                x => x.CreationDate,
                SortDirection.Ascend,
                x => x.DepositionId == id);

            return Result.Ok(depositionEvents);
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

        public async Task<Result<DepositionEvent>> GoOnTheRecord(Guid id, bool onTheRecord, string userEmail)
        {
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Witness), nameof(Deposition.Case), nameof(Deposition.Events)};

            var depositionResult = await GetByIdWithIncludes(id, includes);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<DepositionEvent>();

            var deposition = depositionResult.Value;
            if (deposition.IsOnTheRecord == onTheRecord)
            {
                return Result.Fail(new InvalidInputError($"The current deposition is already in status onTheRecord: {onTheRecord}"));
            }

            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
                return userResult.ToResult<DepositionEvent>();

            var depositionEvent = new DepositionEvent
            {
                EventType = onTheRecord ? EventType.OnTheRecord : EventType.OffTheRecord,
                User = userResult.Value
            };

            deposition.Events.Add(depositionEvent);
            deposition.IsOnTheRecord = onTheRecord;
            var updatedDeposition = await _depositionRepository.Update(deposition);

            return Result.Ok(depositionEvent);
        }

        public async Task<Result<Deposition>> Update(Deposition deposition)
        {
            var oldDeposition = await _depositionRepository.GetById(deposition.Id);
            if (oldDeposition == null)
                return Result.Fail(new ResourceNotFoundError("Deposition not found"));

            return Result.Ok(await _depositionRepository.Update(deposition));
        }

        public async Task<Result<Document>> GetSharedDocument(Guid id)
        {
            var deposition = await _depositionRepository.GetById(id, new[] { $"{nameof(Deposition.SharingDocument)}.{nameof(Document.AddedBy)}" });

            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError("Deposition not found"));
            if (!deposition.SharingDocumentId.HasValue)
                return Result.Fail(new ResourceConflictError("No document is being shared in this deposition"));

            return Result.Ok(deposition.SharingDocument);
        }

        private async Task<Result<Deposition>> GetByIdWithIncludes(Guid id, string[] include = null)
        {
            var deposition = await _depositionRepository.GetById(id, include);
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            return Result.Ok(deposition);
        }

        public async Task<Result<string>> JoinBreakRoom(Guid depositionId, Guid breakRoomId)
        {
            var depositionResult = await GetDepositionById(depositionId);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<string>();

            return await _breakRoomService.JoinBreakRoom(breakRoomId);
        }

        public Task<Result> LeaveBreakRoom(Guid depositionId, Guid breakRoomId)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<BreakRoom>> LockBreakRoom(Guid depositionId, Guid breakRoomId, bool lockRoom)
        {
            var depositionResult = await GetDepositionById(depositionId);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<BreakRoom>();

            return await _breakRoomService.LockBreakRoom(breakRoomId, lockRoom);
        }

        public async Task<Result<List<BreakRoom>>> GetDepositionBreakRooms(Guid id)
        {
            var include = new[] {
                nameof(Deposition.BreakRooms),
                $"{nameof(Deposition.BreakRooms)}.{nameof(BreakRoom.Attendees)}",
                $"{nameof(Deposition.BreakRooms)}.{nameof(BreakRoom.Attendees)}.{nameof(BreakRoomAttendee.User)}"
            };
            var depositionResult = await GetByIdWithIncludes(id, include);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<List<BreakRoom>>();

            var breakRooms = depositionResult.Value.BreakRooms;
            breakRooms.Sort((p, q) => p.Name.CompareTo(q.Name));
            return Result.Ok(breakRooms);
        }

        public async Task<Result<(Participant, bool)>> CheckParticipant(Guid id, string emailAddress)
        {
            var include = new[] { nameof(Deposition.Participants), nameof(Deposition.Witness) };

            var depositionResult = await GetByIdWithIncludes(id, include);

            if (depositionResult.IsFailed)
                return depositionResult.ToResult<(Participant, bool)>();

            var deposition = depositionResult.Value;
            if (deposition.Status == DepositionStatus.Completed
                || deposition.Status == DepositionStatus.Canceled)
                return Result.Fail(new InvalidInputError("The deposition is not longer available"));

            var userResult = await _userService.GetUserByEmail(emailAddress);
            var isUser = userResult.IsSuccess && !userResult.Value.IsGuest;

            var participant = GetParticipantByEmail(deposition, emailAddress);

            return Result.Ok((participant, isUser));
        }

        public async Task<Result<Deposition>> ClearDepositionDocumentSharingId(Guid depositionId)
        {
            var depositionResult = await _depositionRepository.GetById(depositionId);
            depositionResult.SharingDocumentId = null;
            return Result.Ok(await _depositionRepository.Update(depositionResult));            
        }

        private Participant GetParticipantByEmail(Deposition deposition, string emailAddress)
        {
            Participant participant = null;
            if (deposition.Witness != null && deposition.Witness.Email == emailAddress)
                participant = deposition.Witness;

            if (participant == null)
                participant = deposition.Participants.FirstOrDefault(p => p.Email == emailAddress);

            return participant;
        }

        private bool IsDepositionParticipant(Deposition deposition, string emailAddress)
        {
            return GetParticipantByEmail(deposition, emailAddress) != null;
        }

        public async Task<Result<GuestToken>> JoinGuestParticipant(Guid depositionId, Participant guest)
        {
            var include = new[] { nameof(Deposition.Participants), nameof(Deposition.Witness) };

            var depositionResult = await GetByIdWithIncludes(depositionId, include);

            if (depositionResult.IsFailed)
                return depositionResult.ToResult<GuestToken>();

            var deposition = depositionResult.Value;
            if (deposition.Status == DepositionStatus.Completed
                || deposition.Status == DepositionStatus.Canceled)
                return Result.Fail(new InvalidInputError("The deposition is not longer available"));

            var participant = GetParticipantByEmail(deposition, guest.Email);
            if (guest.Role == ParticipantType.Witness 
                && participant == null 
                && deposition.Witness?.UserId != null)
                return Result.Fail(new InvalidInputError("The deposition already has a participant as witness"));

            var userResult = await _userService.AddGuestUser(guest.User);
            if (userResult.IsFailed)
                return userResult.ToResult<GuestToken>();

            if (participant != null)
            {
                userResult.Value.FirstName = guest.Name;
                participant.User = userResult.Value;
                participant.Name = guest.Name;

                await _participantRepository.Update(participant);
                return await _userService.LoginGuestAsync(guest.Email);
            }
            
            if (guest.Role == ParticipantType.Witness)
                deposition.Witness = guest;

            guest.User = userResult.Value;
            deposition.Participants.Add(guest);
            await _depositionRepository.Update(deposition);

            await _permissionService.AddParticipantPermissions(guest);

            return await _userService.LoginGuestAsync(guest.Email);
        }

        public async Task<Result<Guid>> AddParticipant(Guid depositionId, Participant participant)
        {
            var include = new[] { nameof(Deposition.Participants), nameof(Deposition.Witness) };

            var depositionResult = await GetByIdWithIncludes(depositionId, include);

            var deposition = depositionResult.Value;
            if (deposition.Status == DepositionStatus.Completed
                || deposition.Status == DepositionStatus.Canceled)
                return Result.Fail(new InvalidInputError("The deposition is not longer available"));

            var userResult = await _userService.GetUserByEmail(participant.Email);

            if (userResult.IsFailed)
                return userResult.ToResult();

            var participantResult =  GetParticipantByEmail(deposition, userResult.Value.EmailAddress);
            if (participantResult != null)
                return participantResult.Id.ToResult();

            participant.Name = userResult.Value.FirstName;
            participant.Phone = userResult.Value.PhoneNumber;
            participant.User = userResult.Value;

            if (participant.Role == ParticipantType.Witness && deposition.Witness?.UserId != null)
                return Result.Fail(new InvalidInputError("The deposition already has a participant as witness"));

            if (participant.Role == ParticipantType.Witness)
                deposition.Witness = participant;

            deposition.Participants.Add(participant);

            await _depositionRepository.Update(deposition);

            await _permissionService.AddParticipantPermissions(participant);

            return Result.Ok(participant.Id);
        }

        public async Task<Result<Deposition>> GetDepositionByRoomId(Guid roomId)
        {
            var include = new[] { nameof(Deposition.Events), nameof(Deposition.Room) };
            var deposition = await _depositionRepository.GetFirstOrDefaultByFilter(x => x.RoomId == roomId, include);
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with RoomId = {roomId} not found"));

            return Result.Ok(deposition);
        }
    }
}
