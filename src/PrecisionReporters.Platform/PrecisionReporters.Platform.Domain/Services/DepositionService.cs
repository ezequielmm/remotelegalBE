using FluentResults;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
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
        private readonly DocumentConfiguration _documentsConfiguration;
        private readonly IAwsStorageService _awsStorageService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public DepositionService(IDepositionRepository depositionRepository,
            IParticipantRepository participantRepository,
            IDepositionEventRepository depositionEventRepository,
            IUserService userService,
            IRoomService roomService,
            IBreakRoomService breakRoomService,
            IPermissionService permissionService,
            IAwsStorageService awsStorageService,
            IOptions<DocumentConfiguration> documentConfigurations,
            IBackgroundTaskQueue backgroundTaskQueue)
        {
            _awsStorageService = awsStorageService;
            _documentsConfiguration = documentConfigurations.Value ?? throw new ArgumentException(nameof(documentConfigurations));
            _depositionRepository = depositionRepository;
            _participantRepository = participantRepository;
            _depositionEventRepository = depositionEventRepository;
            _userService = userService;
            _roomService = roomService;
            _breakRoomService = breakRoomService;
            _permissionService = permissionService;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        public async Task<List<Deposition>> GetDepositions(Expression<Func<Deposition, bool>> filter = null,
            string[] include = null)
        {
            return await _depositionRepository.GetByFilter(filter, include);
        }

        public async Task<Result<Deposition>> GetDepositionById(Guid id)
        {
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Case), nameof(Deposition.AddedBy),nameof(Deposition.Caption)};
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

            if (!deposition.Participants.Any(x => x.Email == deposition.Requester.EmailAddress))
                deposition.Participants.Add(new Participant(deposition.Requester, ParticipantType.Observer));

            deposition.AddedBy = addedBy;

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

            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants), nameof(Deposition.Case) };

            Expression<Func<Deposition, bool>> filter = x => status == null || x.Status == status;

            if (userResult.IsFailed || !userResult.Value.IsAdmin)
            {
                filter = x => (status == null || x.Status == status) &&
                    (x.Participants.Any(p => p.Email == userEmail)
                        || x.Requester.EmailAddress == userEmail
                        || x.AddedBy.EmailAddress == userEmail);
            }

            Expression<Func<Deposition, object>> orderBy = sortedField switch
            {
                DepositionSortField.Details => x => x.Details,
                DepositionSortField.Status => x => x.Status,
                DepositionSortField.CaseNumber => x => x.Case.CaseNumber,
                DepositionSortField.CaseName => x => x.Case.Name,
                DepositionSortField.Company => x => x.Requester.CompanyName,
                DepositionSortField.Requester => x => x.Requester.FirstName,
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

            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Room), nameof(Deposition.Participants) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            // TODO: Add distributed lock when our infra allows it
            if (deposition.Room.Status == RoomStatus.Created)
            {
                await _roomService.StartRoom(deposition.Room);
            }

            var currentParticipant = deposition.Participants.FirstOrDefault(p => p.User == userResult.Value);
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

        public async Task<Result<Deposition>> EndDeposition(Guid id, string userEmail)
        {
            var currentUser = await _userService.GetUserByEmail(userEmail);
            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Room),
                $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var roomResult = await _roomService.EndRoom(deposition.Room, witness.Email);
            if (roomResult.IsFailed)
                return roomResult.ToResult<Deposition>();

            deposition.CompleteDate = DateTime.UtcNow;
            deposition.Status = DepositionStatus.Completed;
            deposition.EndedById = currentUser.Value.Id;

            var user = await _userService.GetCurrentUserAsync();
            await GoOnTheRecord(id, false, user.EmailAddress);

            var updatedDeposition = await _depositionRepository.Update(deposition);

            await _userService.RemoveGuestParticipants(deposition.Participants);

            var transcriptDto = new DraftTranscriptDto { DepositionId = id, CurrentUserId = currentUser.Value.Id };
            _backgroundTaskQueue.QueueBackgroundWorkItem(transcriptDto);

            return Result.Ok(updatedDeposition);
        }

        public async Task<Result<Participant>> GetDepositionParticipantByEmail(Guid id, string participantEmail)
        {
            var depositionResult = await GetDepositionById(id);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<Participant>();

            var participant = depositionResult.Value.Participants.FirstOrDefault(p => p.Email == participantEmail);

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
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants), nameof(Deposition.Case), nameof(Deposition.Events) };

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
            await _depositionRepository.Update(deposition);

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
            if (depositionResult.Value.IsOnTheRecord)
                return Result.Fail(new InvalidInputError("Deposition is on the record"));

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
            var include = new[] { nameof(Deposition.Participants) };

            var depositionResult = await GetByIdWithIncludes(id, include);

            if (depositionResult.IsFailed)
                return depositionResult.ToResult<(Participant, bool)>();

            var deposition = depositionResult.Value;
            if (deposition.Status == DepositionStatus.Completed
                || deposition.Status == DepositionStatus.Canceled)
                return Result.Fail(new InvalidInputError("The deposition is not longer available"));

            var userResult = await _userService.GetUserByEmail(emailAddress);
            var isUser = userResult.IsSuccess && !userResult.Value.IsGuest;

            var participant = deposition.Participants.FirstOrDefault(p => p.Email == emailAddress);

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

            if (participant == null)
                participant = deposition.Participants.FirstOrDefault(p => p.Email == emailAddress);

            return participant;
        }

        public async Task<Result<GuestToken>> JoinGuestParticipant(Guid depositionId, Participant guest)
        {
            var include = new[] { nameof(Deposition.Participants) };

            var depositionResult = await GetByIdWithIncludes(depositionId, include);

            if (depositionResult.IsFailed)
                return depositionResult.ToResult<GuestToken>();

            var deposition = depositionResult.Value;
            if (deposition.Status == DepositionStatus.Completed
                || deposition.Status == DepositionStatus.Canceled)
                return Result.Fail(new InvalidInputError("The deposition is not longer available"));

            var participant = deposition.Participants.FirstOrDefault(p => p.Email == guest.Email);
            if (guest.Role == ParticipantType.Witness
                && participant == null
                && deposition.Participants.Single(x => x.Role == ParticipantType.Witness).UserId != null)
                return Result.Fail(new InvalidInputError("The deposition already has a participant as witness"));

            var userResult = await _userService.AddGuestUser(guest.User);
            if (userResult.IsFailed)
                return userResult.ToResult<GuestToken>();

            bool shouldAddPermissions;
            if (participant != null)
            {
                shouldAddPermissions = participant.UserId == null;
                userResult.Value.FirstName = guest.Name;
                participant.User = userResult.Value;
                participant.Name = guest.Name;

                guest = await _participantRepository.Update(participant);
            }
            else
            {
                shouldAddPermissions = true;

                if (guest.Role == ParticipantType.Witness)
                    deposition.Participants[deposition.Participants.FindIndex(x => x.Role == ParticipantType.Witness)] =  guest;
                else 
                {
                    guest.User = userResult.Value;
                    deposition.Participants.Add(guest);
                }
                await _depositionRepository.Update(deposition);
            }

            if (shouldAddPermissions)
            {
                await _permissionService.AddParticipantPermissions(guest);
            }

            return await _userService.LoginGuestAsync(guest.Email);
        }

        public async Task<Result<Guid>> AddParticipant(Guid depositionId, Participant participant)
        {
            var include = new[] { nameof(Deposition.Participants) };

            var depositionResult = await GetByIdWithIncludes(depositionId, include);

            var deposition = depositionResult.Value;
            if (deposition.Status == DepositionStatus.Completed
                || deposition.Status == DepositionStatus.Canceled)
                return Result.Fail(new InvalidInputError("The deposition is not longer available"));

            var userResult = await _userService.GetUserByEmail(participant.Email);

            if (userResult.IsFailed)
                return userResult.ToResult();

            var participantResult = deposition.Participants.FirstOrDefault(p => p.Email == userResult.Value.EmailAddress);
            if (participantResult != null)
                return participantResult.Id.ToResult();

            participant.Name = userResult.Value.FirstName;
            participant.Phone = userResult.Value.PhoneNumber;
            participant.User = userResult.Value;

            if (participant.Role == ParticipantType.Witness && deposition.Participants.Single(x => x.Role == ParticipantType.Witness).UserId != null)
                return Result.Fail(new InvalidInputError("The deposition already has a participant as witness"));

            if (participant.Role == ParticipantType.Witness)
                deposition.Participants[deposition.Participants.FindIndex(x => x.Role == ParticipantType.Witness)] =  participant;
            else
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

        public async Task<Result<DepositionVideoDto>> GetDepositionVideoInformation(Guid depositionId)
        {
            var include = new[] { $"{nameof(Deposition.Room)}.{nameof(Room.Composition)}", nameof(Deposition.Events) };
            var depositionResult = await GetByIdWithIncludes(depositionId, include);

            if (depositionResult.IsFailed)
                return depositionResult.ToResult<DepositionVideoDto>();

            var deposition = depositionResult.Value;
            if (deposition.Room?.Composition == null)
                return Result.Fail(new ResourceNotFoundError($"There is no composition for Deposition id: {depositionId}"));

            string url = "";

            if (deposition.Room.Composition.Status == CompositionStatus.Completed)
            {
                var expirationDate = DateTime.UtcNow.AddHours(_documentsConfiguration.PreSignedUrlValidHours);
                url = _awsStorageService.GetFilePublicUri($"{deposition.Room.Composition.SId}.mp4", _documentsConfiguration.PostDepoVideoBucket, expirationDate);
            }

            var depoTotalTime = (int)(deposition.Room.EndDate.Value - deposition.Room.RecordingStartDate.Value).TotalSeconds;
            var onTheRecordTime = getOnTheRecordTime(deposition.Events);
            var depositionVideo = new DepositionVideoDto
            {
                PublicUrl = url,
                TotalTime = depoTotalTime,
                OnTheRecordTime = onTheRecordTime,
                OffTheRecordTime = depoTotalTime - onTheRecordTime,
                Status = deposition.Room.Composition.Status.ToString()
            };

            return Result.Ok(depositionVideo);
        }

        private int getOnTheRecordTime(List<DepositionEvent> events)
        {
            int total = 0;
            events
                .OrderBy(x => x.CreationDate)
                .Where(x => x.EventType == EventType.OnTheRecord || x.EventType == EventType.OffTheRecord)
                .Aggregate(new List<DateTime>(),
                (list, x) =>
                {
                    if (x.EventType == EventType.OnTheRecord)
                        list.Add(x.CreationDate);
                    if (x.EventType == EventType.OffTheRecord)
                    {
                        total += (int)(x.CreationDate - list.Last()).TotalSeconds;
                        list.Add(x.CreationDate);
                    }

                    return list;
                });
            return total;
        }

        public async Task<Result<Document>> GetDepositionCaption(Guid id)
        {
            var deposition = await _depositionRepository.GetById(id, new[] { $"{nameof(Deposition.Caption)}" });

            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError("Deposition not found"));
            if (!deposition.CaptionId.HasValue)
                return Result.Fail(new ResourceNotFoundError("Caption not found in this deposition"));

            return Result.Ok(deposition.Caption);
        }

        public async Task<Result<List<Participant>>> GetDepositionParticipants(Guid depositionId,
            ParticipantSortField sortedField,
            SortDirection sortDirection)
        {
            Expression<Func<Participant, object>> orderBy = sortedField switch
            {
                ParticipantSortField.Role => x => x.Role,
                ParticipantSortField.Name => x => x.Name,
                ParticipantSortField.Email => x => x.Email,
                _ => x => x.Role
            };
            var lstParticipant = await _participantRepository.GetByFilter(orderBy,
                sortDirection,
                x => x.DepositionId == depositionId,
                new string[] {nameof(Participant.User)});
            return Result.Ok(lstParticipant);
        }

        public async Task<Result<Participant>> AddParticipantToExistingDeposition(Guid id, Participant participant)
        {
            var deposition = await _depositionRepository.GetById(id, new[] { $"{nameof(Deposition.Participants)}" });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError("Deposition not found"));

            if (deposition.Participants.Any(x => !string.IsNullOrWhiteSpace(participant.Email) && x.Email == participant.Email))
                return Result.Fail(new InvalidInputError("Participant already exists"));

            if ((participant.Role == ParticipantType.Witness ||
                participant.Role == ParticipantType.CourtReporter) &&
                deposition.Participants.Any(x => x.Role == participant.Role))
            {
                var role = participant.Role == ParticipantType.Witness ? "witness" : "court reporter";
                return Result.Fail(new InvalidInputError($"The deposition already has a participant as {role}"));
            }

            var newParticipant = new Participant();
            newParticipant.CopyFrom(participant);
            var userResult = await _userService.GetUserByEmail(participant.Email);
            if (userResult.IsSuccess)
            {
                newParticipant.User = userResult.Value;
            }
            deposition.Participants.Add(newParticipant);
            await _depositionRepository.Update(deposition);
            await _permissionService.AddParticipantPermissions(newParticipant);

            return Result.Ok(newParticipant);
        }

        public async Task<Result> RemoveParticipantFromDeposition(Guid id, Guid participantId)
        {
            var deposition = await _depositionRepository.GetById(id,
                new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition not found with ID {id}"));

            var participant = deposition.Participants.FirstOrDefault(x => x.Id == participantId);
            if (participant == null)
                return Result.Fail(new ResourceNotFoundError($"Participant not found with ID {participantId}"));

            await _permissionService.RemoveParticipantPermissions(id, participant);
            await _participantRepository.Remove(participant);
            return Result.Ok();
        }
    }
}
