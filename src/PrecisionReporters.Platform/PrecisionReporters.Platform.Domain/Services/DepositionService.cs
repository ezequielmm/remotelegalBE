using FluentResults;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using LinqKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
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
        private const string DOWNLOAD_TRANSCRIPT_TEMPLATE = "DownloadCertifiedTranscriptEmailTemplate";
        private const string DOWNLOAD_ASSETS_TEMPLATE = "DownloadAssetsEmailTemplate";
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
        private readonly ITransactionHandler _transactionHandler;
        private readonly ILogger<DepositionService> _logger;
        private readonly IDocumentService _documentService;
        private readonly IMapper<Deposition, DepositionDto, CreateDepositionDto> _depositionMapper;
        private readonly IMapper<Participant, ParticipantDto, CreateParticipantDto> _participantMapper;
        private readonly IMapper<BreakRoom, BreakRoomDto, object> _breakRoomMapper;
        private readonly DepositionConfiguration _depositionConfiguration;
        private readonly ISignalRNotificationManager _signalRNotificationManager;
        private readonly IAwsEmailService _awsEmailService;
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private readonly EmailConfiguration _emailConfiguration;
        private readonly IActivityHistoryService _activityHistoryService;

        public DepositionService(IDepositionRepository depositionRepository,
            IParticipantRepository participantRepository,
            IDepositionEventRepository depositionEventRepository,
            IUserService userService,
            IRoomService roomService,
            IBreakRoomService breakRoomService,
            IPermissionService permissionService,
            IAwsStorageService awsStorageService,
            IOptions<DocumentConfiguration> documentConfigurations,
            IBackgroundTaskQueue backgroundTaskQueue,
            ITransactionHandler transactionHandler,
            ILogger<DepositionService> logger,
            IDocumentService documentService,
            IMapper<Deposition, DepositionDto, CreateDepositionDto> depositionMapper,
            IMapper<Participant, ParticipantDto, CreateParticipantDto> participantMapper,
            IOptions<DepositionConfiguration> depositionConfiguration,
            ISignalRNotificationManager signalRNotificationManager,
            IAwsEmailService awsEmailService,
            IOptions<UrlPathConfiguration> urlPathConfiguration,
            IOptions<EmailConfiguration> emailConfiguration,
            IMapper<BreakRoom, BreakRoomDto, object> breakRoomMapper,
            IActivityHistoryService activityHistoryService)
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
            _transactionHandler = transactionHandler;
            _logger = logger;
            _documentService = documentService;
            _depositionMapper = depositionMapper;
            _participantMapper = participantMapper;
            _depositionConfiguration = depositionConfiguration.Value;
            _signalRNotificationManager = signalRNotificationManager;
            _awsEmailService = awsEmailService;
            _urlPathConfiguration = urlPathConfiguration.Value;
            _emailConfiguration = emailConfiguration.Value;
            _breakRoomMapper = breakRoomMapper;
            _activityHistoryService = activityHistoryService;
        }

        public async Task<List<Deposition>> GetDepositions(Expression<Func<Deposition, bool>> filter = null,
            string[] include = null)
        {
            return await _depositionRepository.GetByFilter(filter, include);
        }

        public async Task<Result<Deposition>> GetDepositionById(Guid id)
        {
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Case), nameof(Deposition.AddedBy),nameof(Deposition.Caption), nameof(Deposition.Events)};
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
            if (!addedBy.IsAdmin)
                deposition.Requester = addedBy;
            else
            {
                var requesterResult = await _userService.GetUserByEmail(deposition.Requester.EmailAddress);
                if (requesterResult.IsFailed)
                {
                    return Result.Fail(new ResourceNotFoundError($"Requester with email {deposition.Requester.EmailAddress} not found"));
                }
                deposition.Requester = requesterResult.Value;
            }

            //Adding Requester as a Participant
            if (deposition.Participants == null)
                deposition.Participants = new List<Participant>();

            if (!deposition.Participants.Any(x => x.Email == deposition.Requester.EmailAddress))
                deposition.Participants.Add(new Participant(deposition.Requester, ParticipantType.Observer, true));

            deposition.AddedBy = addedBy;

            // If caption has a FileKey, find the matching document. If it doesn't has a FileKey, remove caption
            deposition.Caption = !string.IsNullOrWhiteSpace(deposition.FileKey) ? uploadedDocuments.First(d => d.FileKey == deposition.FileKey) : null;

            deposition.Room = new Room(deposition.Id.ToString(), deposition.IsVideoRecordingNeeded);
            deposition.PreRoom = new Room($"{ deposition.Id}-pre", false);

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
                        participant.Name = user.IsGuest ? user.FirstName : $"{user.FirstName} {user.LastName}";
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

        public async Task<DepositionFilterResponseDto> GetDepositionsByStatus(DepositionFilterDto filterDto)
        {
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants), nameof(Deposition.Case) };

            var filter = await GetDepositionsFilter(filterDto, filterDto.PastDepositions);
            var orderByQuery = GetDepositionsOrderBy(filterDto);
            var result = await _depositionRepository.GetByFilterPagination(filter, orderByQuery.Compile(), includes, filterDto.Page, filterDto.PageSize);

            var filterCount = await GetDepositionsFilter(filterDto, !filterDto.PastDepositions);
            var count = await _depositionRepository.GetCountByFilter(filterCount);

            var response = new DepositionFilterResponseDto
            {
                TotalUpcoming = filterDto.PastDepositions ? count : result.Item1,
                TotalPast = filterDto.PastDepositions ? result.Item1 : count,
                Page = filterDto.Page,
                NumberOfPages = (result.Item1 + filterDto.PageSize - 1) / filterDto.PageSize,
                Depositions = result.Item2?.Select(c => _depositionMapper.ToDto(c)).ToList()
            };
            return response;
        }

        private async Task<Expression<Func<Deposition, bool>>> GetDepositionsFilter(DepositionFilterDto filterDto, bool pastDeposition)
        {
            var user = await _userService.GetCurrentUserAsync();
            var exp = PredicateBuilder.New<Deposition>(true);
            exp.And(x => filterDto.Status == null || x.Status == filterDto.Status);

            if (filterDto.MaxDate.HasValue && filterDto.MinDate.HasValue)
                exp.And(x => x.StartDate < filterDto.MaxDate.Value.UtcDateTime && x.StartDate > filterDto.MinDate.Value.UtcDateTime);

            if (!user.IsAdmin)
            {
                exp.And(x => (x.Status != DepositionStatus.Canceled &&
                         (x.Participants.Any(p => p.Email == user.EmailAddress && p.IsAdmitted.HasValue && p.IsAdmitted.Value)
                         || x.Requester.EmailAddress == user.EmailAddress
                         || x.AddedBy.EmailAddress == user.EmailAddress)));
            }

            exp.And(x => pastDeposition ? x.StartDate < DateTime.UtcNow : x.StartDate > DateTime.UtcNow);
            return exp;
        }

        private Expression<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>> GetDepositionsOrderBy(DepositionFilterDto filterDto)
        {
            Expression<Func<Deposition, object>> orderBy = filterDto.SortedField switch
            {
                DepositionSortField.Details => x => x.Details,
                DepositionSortField.Status => x => x.Status,
                DepositionSortField.CaseNumber => x => x.Case.CaseNumber,
                DepositionSortField.CaseName => x => x.Case.Name,
                DepositionSortField.Company => x => x.Requester.CompanyName,
                DepositionSortField.Requester => x => x.Requester.FirstName,
                DepositionSortField.Job => x => x.Job,
                _ => x => x.StartDate,
            };
            Expression<Func<Deposition, object>> orderByThen = x => x.Requester.LastName;
            Expression<Func<Deposition, object>> orderByDefault = x => x.StartDate;
            Expression<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>> orderByQuery = null;
            if (filterDto.SortedField == DepositionSortField.Requester)
            {
                if (filterDto.SortDirection == null || filterDto.SortDirection == SortDirection.Ascend)
                {
                    orderByQuery = d => d.OrderBy(orderBy).ThenBy(orderByThen).ThenBy(orderByDefault);
                }
                else
                {
                    orderByQuery = d => d.OrderByDescending(orderBy).ThenByDescending(orderByThen).ThenBy(orderByDefault);
                }
            }
            else
            {
                if (filterDto.SortDirection == null || filterDto.SortDirection == SortDirection.Ascend)
                {
                    orderByQuery = d => d.OrderBy(orderBy).ThenBy(orderByDefault);
                }
                else
                {
                    orderByQuery = d => d.OrderByDescending(orderBy).ThenBy(orderByDefault);
                }
            }
            return orderByQuery;
        }

        public async Task<Result<JoinDepositionDto>> JoinDeposition(Guid id, string identity)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult<JoinDepositionDto>();

            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Room), nameof(Deposition.PreRoom), nameof(Deposition.Participants) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            var joinDepositionInfo = await GetJoinDepositionInfoDto(userResult.Value, deposition, identity);

            if (joinDepositionInfo.IsFailed)
                return Result.Fail(new UnexpectedError($"There was an issue trying to Join the deposition."));

            return Result.Ok(joinDepositionInfo.Value);
        }

        public async Task<Result<Deposition>> EndDeposition(Guid depositionId)
        {
            // TODO: use distributed lock
            var include = new[] { nameof(Deposition.Room), $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}",
                nameof(Deposition.AddedBy) };
            var deposition = await _depositionRepository.GetById(depositionId, include);

            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {depositionId} not found."));

            var currentUser = await _userService.GetCurrentUserAsync();
            var transactionResult = await _transactionHandler.RunAsync<Deposition>(async () =>
            {
                var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
                var roomResult = await _roomService.EndRoom(deposition.Room, witness.Email);
                if (roomResult.IsFailed)
                    return roomResult.ToResult<Deposition>();

                deposition.CompleteDate = DateTime.UtcNow;
                deposition.Status = DepositionStatus.Completed;
                deposition.EndedById = currentUser?.Id;

                var email = currentUser != null ? currentUser.EmailAddress : deposition.AddedBy.EmailAddress;
                var updatedDeposition = await _depositionRepository.Update(deposition);

                await GoOnTheRecord(deposition.Id, false, email);
                await _userService.RemoveGuestParticipants(deposition.Participants);

                return Result.Ok(updatedDeposition);
            });
            if (transactionResult.IsFailed)
                return transactionResult;

            var notification = new NotificationDto
            {
                Action = NotificationAction.Update,
                EntityType = NotificationEntity.EndDeposition
            };
            await _signalRNotificationManager.SendNotificationToDepositionMembers(depositionId, notification);

            var transcriptDto = new DraftTranscriptDto { DepositionId = deposition.Id, CurrentUserId = currentUser.Id };
            var backGround = new BackgroundTaskDto() { Content = transcriptDto, TaskType = BackgroundTaskType.DraftTranscription };
            _backgroundTaskQueue.QueueBackgroundWorkItem(backGround);
            await NotifyParties(deposition.Id, true);

            return transactionResult;
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

        public async Task<Result<Deposition>> GetByIdWithIncludes(Guid id, string[] include = null)
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

            var currentUser = await _userService.GetCurrentUserAsync();
            var currentParticipant = depositionResult.Value.Participants.FirstOrDefault(p => p.UserId == currentUser.Id);
            if (currentParticipant == null && !currentUser.IsAdmin)
                return Result.Fail(new InvalidInputError($"User is neither a Participant for this Deposition nor an Admin"));

            if (currentParticipant == null && currentUser.IsAdmin)
            {
                currentParticipant = new Participant()
                {
                    User = currentUser,
                    UserId = currentUser.Id,
                    Role = ParticipantType.Admin
                };
            }

            return await _breakRoomService.JoinBreakRoom(breakRoomId, currentParticipant);
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

            var result = await _breakRoomService.LockBreakRoom(breakRoomId, lockRoom);
            if (result.IsSuccess)
            {
                var notificationtDto = new NotificationDto
                {
                    Action = lockRoom ? NotificationAction.Create : NotificationAction.Update,
                    EntityType = NotificationEntity.LockBreakRoom,
                    Content = _breakRoomMapper.ToDto(result.Value)
                };
                await _signalRNotificationManager.SendNotificationToDepositionMembers(depositionId, notificationtDto);
            }

            return result;
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

        public async Task<Result<GuestToken>> JoinGuestParticipant(Guid depositionId, Participant guest, ActivityHistory activityHistory)
        {
            var include = new[] { nameof(Deposition.Participants), nameof(Deposition.Case) };

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
            bool shouldSendAdminsNotifications = false;
            if (participant != null)
            {
                shouldAddPermissions = participant.UserId == null;
                userResult.Value.FirstName = guest.Name;
                participant.User = userResult.Value;
                participant.Name = guest.Name;
                if (participant.IsAdmitted.HasValue && !participant.IsAdmitted.Value)
                {
                    participant.IsAdmitted = null;
                    shouldSendAdminsNotifications = true;
                }
                guest = await _participantRepository.Update(participant);
            }
            else
            {
                shouldAddPermissions = true;
                guest.User = userResult.Value;
                if (guest.Role == ParticipantType.Witness)
                    deposition.Participants[deposition.Participants.FindIndex(x => x.Role == ParticipantType.Witness)] = guest;
                else
                {
                    deposition.Participants.Add(guest);
                }
                shouldSendAdminsNotifications = true;
                await _depositionRepository.Update(deposition);
            }

            if (shouldAddPermissions)
            {
                await _permissionService.AddParticipantPermissions(guest);
            }
            if (shouldSendAdminsNotifications)
            {
                var notificationtDto = new NotificationDto
                {
                    Action = NotificationAction.Create,
                    EntityType = NotificationEntity.JoinRequest,
                    Content = _participantMapper.ToDto(guest)
                };
                await _signalRNotificationManager.SendNotificationToDepositionAdmins(depositionId, notificationtDto);
            }

            await _activityHistoryService.AddActivity(activityHistory, userResult.Value, deposition);

            return await _userService.LoginGuestAsync(guest.Email);
        }

        public async Task<Result<Guid>> AddParticipant(Guid depositionId, Participant participant)
        {
            var include = new[] { nameof(Deposition.Participants), nameof(Deposition.Case) };

            var depositionResult = await GetByIdWithIncludes(depositionId, include);

            var deposition = depositionResult.Value;
            if (deposition.Status == DepositionStatus.Completed
                || deposition.Status == DepositionStatus.Canceled)
                return Result.Fail(new InvalidInputError("The deposition is not longer available"));

            var userResult = await _userService.GetUserByEmail(participant.Email);

            if (userResult.IsFailed)
                return userResult.ToResult();

            var notificationtDto = new NotificationDto
            {
                Action = NotificationAction.Create,
                EntityType = NotificationEntity.JoinRequest
            };

            var participantResult = deposition.Participants.FirstOrDefault(p => p.Email == userResult.Value.EmailAddress);
            if (participantResult != null)
            {
                if (participantResult.IsAdmitted.HasValue && !participantResult.IsAdmitted.Value && !userResult.Value.IsAdmin)
                {
                    participantResult.IsAdmitted = null;
                    notificationtDto.Content = _participantMapper.ToDto(participantResult);
                    await _signalRNotificationManager.SendNotificationToDepositionAdmins(depositionId, notificationtDto);
                }
                if (userResult.Value.IsAdmin)
                    participantResult.IsAdmitted = true;

                await _participantRepository.Update(participantResult);
                return participantResult.Id.ToResult();
            }
            if (userResult.Value.IsAdmin)
                participant.IsAdmitted = true;

            participant.Name = userResult.Value.IsGuest ? userResult.Value.FirstName : $"{userResult.Value.FirstName} {userResult.Value.LastName}";
            participant.Phone = userResult.Value.PhoneNumber;
            participant.User = userResult.Value;

            if (participant.Role == ParticipantType.Witness && deposition.Participants.Single(x => x.Role == ParticipantType.Witness).UserId != null)
                return Result.Fail(new InvalidInputError("The deposition already has a participant as witness"));

            if (participant.Role == ParticipantType.Witness)
                deposition.Participants[deposition.Participants.FindIndex(x => x.Role == ParticipantType.Witness)] = participant;
            else
                deposition.Participants.Add(participant);

            await _depositionRepository.Update(deposition);

            await _permissionService.AddParticipantPermissions(participant);
            notificationtDto.Content = _participantMapper.ToDto(participant);
            await _signalRNotificationManager.SendNotificationToDepositionAdmins(depositionId, notificationtDto);

            return Result.Ok(participant.Id);
        }

        public async Task<Result<Deposition>> GetDepositionByRoomId(Guid roomId)
        {
            var include = new[] { nameof(Deposition.Events), nameof(Deposition.Room), nameof(Deposition.Participants) };
            var deposition = await _depositionRepository.GetFirstOrDefaultByFilter(x => x.RoomId == roomId, include);
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with RoomId = {roomId} not found"));

            return Result.Ok(deposition);
        }

        public async Task<Result<DepositionVideoDto>> GetDepositionVideoInformation(Guid depositionId)
        {
            var include = new[] { $"{nameof(Deposition.Room)}.{nameof(Room.Composition)}", nameof(Deposition.Events), nameof(Deposition.Case), nameof(Deposition.Participants) };
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

                var fileName = deposition.Case.Name;
                var witness = deposition.Participants?.FirstOrDefault(x => x.Role == ParticipantType.Witness);
                if (!string.IsNullOrEmpty(witness?.Name))
                    fileName += $"-{witness.Name}";

                url = _awsStorageService.GetFilePublicUri($"{deposition.Room.Composition.SId}.{deposition.Room.Composition.FileType}", _documentsConfiguration.PostDepoVideoBucket, expirationDate, $"{fileName}.{deposition.Room.Composition.FileType}");
            }
            var depoStartDate = deposition.GetActualStartDate() ?? deposition.Room.RecordingStartDate;
            var depoTotalTime = (int)(deposition.Room.EndDate.Value - depoStartDate.Value).TotalSeconds;
            var onTheRecordTime = GetOnTheRecordTime(deposition.Events);
            var depositionVideo = new DepositionVideoDto
            {
                PublicUrl = url,
                TotalTime = depoTotalTime,
                OnTheRecordTime = onTheRecordTime,
                OffTheRecordTime = depoTotalTime - onTheRecordTime,
                Status = deposition.Room.Composition.Status.ToString(),
                OutputFormat = deposition.Room.Composition.FileType
            };

            return Result.Ok(depositionVideo);
        }

        private int GetOnTheRecordTime(List<DepositionEvent> events)
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
                x => x.DepositionId == depositionId && x.IsAdmitted.HasValue && x.IsAdmitted.Value,
                new string[] { nameof(Participant.User) });
            return Result.Ok(lstParticipant);
        }

        public async Task<Result<Participant>> AddParticipantToExistingDeposition(Guid id, Participant participant)
        {
            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Participants), nameof(Deposition.Case) });
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
            newParticipant.IsAdmitted = true;
            deposition.Participants.Add(newParticipant);
            await _depositionRepository.Update(deposition);
            await _permissionService.AddParticipantPermissions(newParticipant);

            if (deposition.Status == DepositionStatus.Confirmed && !string.IsNullOrWhiteSpace(newParticipant.Email))
                await SendJoinDepositionEmailNotification(deposition, newParticipant);

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

        private async Task<Result<Document>> UpdateDepositionFiles(FileTransferInfo file, Deposition currentDeposition, bool deleteCaption)
        {
            var currentUser = await _userService.GetCurrentUserAsync();
            var uploadDocumentResult = await UploadFile(file, currentUser, currentDeposition);

            if (uploadDocumentResult.IsFailed)
                return uploadDocumentResult.ToResult();

            if (currentDeposition.Caption != null && deleteCaption)
            {
                await _documentService.DeleteUploadedFiles(new List<Document>() { currentDeposition.Caption });
                currentDeposition.Caption = null;
            }

            return uploadDocumentResult;
        }

        private Deposition UpdateDepositionDetails(Deposition currentDeposition, Deposition deposition, Document caption)
        {
            currentDeposition.Job = deposition.Job;
            currentDeposition.Details = deposition.Details;
            currentDeposition.IsVideoRecordingNeeded = deposition.IsVideoRecordingNeeded;
            currentDeposition.Status = deposition.Status;
            currentDeposition.Caption = caption ?? currentDeposition.Caption;

            return currentDeposition;
        }

        public async Task<Result<Deposition>> EditDepositionDetails(Deposition deposition, FileTransferInfo file, bool deleteCaption)
        {
            var currentDepositionResult = await GetByIdWithIncludes(deposition.Id, new[] { nameof(Deposition.Caption), nameof(Deposition.Participants), nameof(Deposition.Case) });
            if (currentDepositionResult.IsFailed)
                return currentDepositionResult;

            var currentDeposition = currentDepositionResult.Value;
            var sendEmailNotification = false;

            try
            {
                var transactionResult = await _transactionHandler.RunAsync<Deposition>(async () =>
                {
                    if (deposition.RequesterNotes != null)
                    {
                        currentDeposition.RequesterNotes = deposition.RequesterNotes;
                    }
                    else
                    {
                        var uploadDocumentResult = await UpdateDepositionFiles(file, currentDeposition, deleteCaption);
                        if (uploadDocumentResult.IsFailed)
                            return uploadDocumentResult.ToResult();

                        if (currentDeposition.Status != DepositionStatus.Confirmed && deposition.Status == DepositionStatus.Confirmed)
                            sendEmailNotification = true;

                        currentDeposition = UpdateDepositionDetails(currentDeposition, deposition, uploadDocumentResult.Value);
                    }

                    await _depositionRepository.Update(currentDeposition);
                    if (sendEmailNotification)
                        await SendJoinDepositionEmailNotification(currentDeposition);
                    return Result.Ok(currentDeposition);
                });

                return transactionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to edit depositions");
                await _documentService.DeleteUploadedFiles(new List<Document> { currentDeposition.Caption });
                return Result.Fail(new ExceptionalError("Unable to edit the deposition", ex));
            }
        }

        public async Task<Result<DepositionFilterResponseDto>> GetDepositionsByFilter(DepositionFilterDto filterDto)
        {
            if ((filterDto.MinDate.HasValue && !filterDto.MaxDate.HasValue) || (filterDto.MaxDate.HasValue && !filterDto.MinDate.HasValue) ||
                ((filterDto.MinDate.HasValue && filterDto.MaxDate.HasValue) && (filterDto.MinDate.Value > filterDto.MaxDate.Value)))
            {
                return Result.Fail(new InvalidInputError("Invalid range of dates"));
            }
            var response = await GetDepositionsByStatus(filterDto);
            return Result.Ok(response);
        }

        private async Task<Result<Document>> UploadFile(FileTransferInfo file, User user, Deposition deposition)
        {
            if (file != null)
            {
                var documentResult = await _documentService.UploadDocumentFile(file, user, $"{deposition.CaseId}/caption", DocumentType.Caption);
                if (documentResult.IsFailed)
                {
                    var uploadedDocuments = new List<Document>();
                    uploadedDocuments.Add(documentResult.Value);
                    _logger.LogError(new Exception(documentResult.Errors.First().Message), "Unable to load one or more documents to storage");
                    _logger.LogInformation("Removing uploaded documents");
                    await _documentService.DeleteUploadedFiles(uploadedDocuments);
                    return Result.Fail(new Error("Unable to upload one or more documents to deposition"));
                }
                return documentResult;
            }
            return Result.Ok((Document)null);
        }

        public async Task<Result<Deposition>> CancelDeposition(Guid depositionId)
        {
            var depositionResult = await GetDepositionById(depositionId);
            if (depositionResult.IsFailed)
                return depositionResult;

            var sendNotification = depositionResult.Value.Status == DepositionStatus.Confirmed;
            var cancelTime = int.Parse(_depositionConfiguration.CancelAllowedOffsetSeconds);
            if (depositionResult.Value.StartDate < DateTime.UtcNow.AddSeconds(cancelTime))
                return Result.Fail<Deposition>(new ResourceConflictError($"The depostion with id {depositionId} can not be canceled because is close to start"));

            depositionResult.Value.Status = DepositionStatus.Canceled;
            var depositionUpdated = await _depositionRepository.Update(depositionResult.Value);

            if (sendNotification)
            {
                var tasks = depositionResult.Value.Participants.Where(p => !string.IsNullOrWhiteSpace(p.Email)).Select(participant => SendCancelDepositionEmailNotification(depositionResult.Value, participant));
                await Task.WhenAll(tasks);
            }

            return Result.Ok(depositionUpdated);
        }

        public async Task<Result<Deposition>> RevertCancel(Deposition deposition, FileTransferInfo file, bool deleteCaption)
        {
            var currentDepositionResult = await GetByIdWithIncludes(deposition.Id, new[] { $"{nameof(Deposition.Caption)}" });
            if (currentDepositionResult.IsFailed)
                return currentDepositionResult;

            var currentDeposition = currentDepositionResult.Value;
            try
            {
                var transactionResult = await _transactionHandler.RunAsync<Deposition>(async () =>
                {
                    var uploadDocumentResult = await UpdateDepositionFiles(file, currentDeposition, deleteCaption);
                    if (uploadDocumentResult.IsFailed)
                        return uploadDocumentResult.ToResult();

                    currentDeposition = UpdateDepositionDetails(currentDeposition, deposition, uploadDocumentResult.Value);

                    await _depositionRepository.Update(currentDeposition);
                    return Result.Ok(currentDeposition);
                });

                return transactionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to edit depositions");
                await _documentService.DeleteUploadedFiles(new List<Document> { currentDeposition.Caption });
                return Result.Fail(new ExceptionalError("Unable to edit the deposition", ex));
            }
        }

        public async Task<Result<Participant>> GetUserParticipant(Guid depositionId)
        {
            var includes = new[] { nameof(Participant.User) };
            var currentUser = await _userService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                var participant = await _participantRepository.GetFirstOrDefaultByFilter(x => x.DepositionId == depositionId && x.UserId == currentUser.Id, includes);
                if (participant == null)
                    participant = new Participant() { User = currentUser };
                return Result.Ok(participant);
            }
            return Result.Fail(new ResourceNotFoundError("User not found"));
        }

        public async Task<Result> AdmitDenyParticipant(Guid participantId, bool admited)
        {
            var include = new[] { nameof(Participant.User) };
            var participant = await _participantRepository.GetById(participantId, include);
            if (participant == null)
                return Result.Fail(new ResourceNotFoundError($"Participant not found with Id: {participantId}"));
            participant.IsAdmitted = admited;
            var notificationtDto = new NotificationDto
            {
                Action = NotificationAction.Update,
                EntityType = NotificationEntity.JoinResponse,
                Content = _participantMapper.ToDto(participant)
            };
            if (!admited)
            {
                await _permissionService.RemoveParticipantPermissions(participant.DepositionId.Value, participant);
            }
            else
            {
                await _permissionService.AddParticipantPermissions(participant);
            }
            await _participantRepository.Update(participant);
            await _signalRNotificationManager.SendDirectMessage(participant.Email, notificationtDto);
            await _signalRNotificationManager.SendNotificationToDepositionAdmins(participant.DepositionId.Value, notificationtDto);
            return Result.Ok();
        }

        public async Task<Result<Deposition>> ReScheduleDeposition(Deposition deposition, FileTransferInfo file, bool deleteCaption)
        {
            var minReScheduleTime = int.Parse(_depositionConfiguration.MinimumReScheduleSeconds);
            if (deposition.StartDate < DateTime.UtcNow.AddSeconds(minReScheduleTime))
                return Result.Fail<Deposition>(new ResourceConflictError($"The StartDate is lower than the minimum time to re-schedule"));

            if (deposition.StartDate > deposition.EndDate)
                return Result.Fail<Deposition>(new ResourceConflictError($"The StartDate must be lower than EndDate"));

            var currentDepositionResult = await GetByIdWithIncludes(deposition.Id, new[] { nameof(Deposition.Case), nameof(Deposition.Caption), nameof(Deposition.Participants) });
            if (currentDepositionResult.IsFailed)
                return currentDepositionResult;

            var currentDeposition = currentDepositionResult.Value;

            var oldStartDate = currentDeposition.StartDate;
            var oldTimeZone = currentDeposition.TimeZone;

            try
            {
                var transactionResult = await _transactionHandler.RunAsync<Deposition>(async () =>
                {
                    var uploadDocumentResult = await UpdateDepositionFiles(file, currentDeposition, deleteCaption);
                    if (uploadDocumentResult.IsFailed)
                        return uploadDocumentResult.ToResult();

                    currentDeposition = UpdateDepositionDetails(currentDeposition, deposition, uploadDocumentResult.Value);

                    currentDeposition.StartDate = deposition.StartDate;
                    currentDeposition.EndDate = deposition.EndDate;
                    currentDeposition.TimeZone = deposition.TimeZone;

                    await _depositionRepository.Update(currentDeposition);

                    if (currentDeposition.Status == DepositionStatus.Confirmed)
                    {
                        var tasks = currentDeposition.Participants.Where(p => !string.IsNullOrWhiteSpace(p.Email)).Select(participant => SendReSheduleDepositionEmailNotification(currentDeposition, participant, oldStartDate, oldTimeZone));
                        await Task.WhenAll(tasks);
                    }

                    return Result.Ok(currentDeposition);
                });

                return transactionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to edit depositions");
                await _documentService.DeleteUploadedFiles(new List<Document> { currentDeposition.Caption });
                return Result.Fail(new ExceptionalError("Unable to edit the deposition", ex));
            }
        }

        public async Task<Result<bool>> NotifyParties(Guid depositionId, bool isEndDeposition = false)
        {
            var depositionResult = await GetDepositionById(depositionId);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult();

            var participants = depositionResult.Value.Participants.Where(x => x.Role != ParticipantType.Witness);
            if (!participants.Any())
                return Result.Fail(new ResourceConflictError($"The deposition {depositionId} must have participants"));

            var witness = depositionResult.Value.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            if (witness == null)
                return Result.Fail(new ResourceConflictError($"The Deposition {depositionId} must have a witness"));

            var startDate = depositionResult.Value.GetActualStartDate() ?? depositionResult.Value.StartDate;

            try
            {
                foreach (var participant in participants.Where(p => !string.IsNullOrWhiteSpace(p.Email)))
                {
                    if (participant.Role != ParticipantType.Witness)
                    {
                        var template = new EmailTemplateInfo
                        {
                            EmailTo = new List<string> { participant.Email },
                            TemplateData = new Dictionary<string, string>
                            {
                                { "user-name", participant.Name },
                                { "witness-name", witness.Name },
                                { "case-name", depositionResult.Value.Case.Name },
                                { "start-date",  startDate.GetFormattedDateTime(depositionResult.Value.TimeZone)},
                                { "depo-details-link", $"{_urlPathConfiguration.FrontendBaseUrl}deposition/post-depo-details/{depositionResult.Value.Id}" },
                                { "logo", $"{_emailConfiguration.ImagesUrl}{_emailConfiguration.LogoImageName}"},
                                { "calendar", $"{_emailConfiguration.ImagesUrl}{_emailConfiguration.CalendarImageName}"}
                            },
                            TemplateName = isEndDeposition ? DOWNLOAD_ASSETS_TEMPLATE : DOWNLOAD_TRANSCRIPT_TEMPLATE
                        };

                        await _awsEmailService.SetTemplateEmailRequest(template, _emailConfiguration.EmailNotification);
                    }
                }

                return Result.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to notify parties");
                return Result.Ok(false);
            }
        }

        //TODO: Check concurrencies
        private async Task<Result<JoinDepositionDto>> GetJoinDepositionInfoDto(User user, Deposition deposition, string identity)
        {
            var currentParticipant = deposition.Participants.FirstOrDefault(p => p.User == user);
            var role = currentParticipant?.Role ?? ParticipantType.Admin;
            var courtReporters = deposition.Participants.Where(x => x.Role == ParticipantType.CourtReporter).ToList();
            var isCourtReporterUser = courtReporters.Any(x => x.User?.Id == user.Id);
            var isCourtReporterJoined = courtReporters.Any(x => x.HasJoined == true);
            var joinDepositionInfo = new JoinDepositionDto();

            // Check if we need to Start a Deposition. Only start it if current user is a Court Reporter and also any other court reporter hasn't joined yet.
            if (isCourtReporterUser && !isCourtReporterJoined)
            {
                // Court Reporter Flow
                await StartDepositionRoom(deposition.Room, true);
                var chatInfo = new ChatDto()
                {
                    AddParticipant = true,
                    ChatName = deposition.Id.ToString(),
                    SId = deposition.ChatSid,
                    CreateChat = true
                };
                var token = await _roomService.GenerateRoomToken(deposition.Room.Name, user, role, identity, chatInfo);
                if (token.IsFailed)
                    return token.ToResult<JoinDepositionDto>();

                joinDepositionInfo = SetJoinDepositionInfo(token.Value, deposition, joinDepositionInfo, false);

                if (currentParticipant != null)
                {
                    await UpdateCurrentParticipant(currentParticipant);
                }

                await SendNotification(deposition);
            }
            else
            {
                // Participants Flow
                if (isCourtReporterJoined)
                {
                    if (currentParticipant != null && !currentParticipant.IsAdmitted.HasValue && !user.IsAdmin)
                    {
                        joinDepositionInfo = SetJoinDepositionInfo(null, deposition, joinDepositionInfo, false);
                    }
                    else
                    {
                        if (currentParticipant != null && currentParticipant.IsAdmitted.HasValue && !currentParticipant.IsAdmitted.Value && !user.IsAdmin)
                            return Result.Fail(new InvalidInputError($"User has not been admitted to the deposition"));

                        if (currentParticipant != null)
                        {
                            await UpdateCurrentParticipant(currentParticipant);
                        }
                        var chatInfo = new ChatDto()
                        {
                            AddParticipant = true,
                            ChatName = deposition.Id.ToString(),
                            SId = deposition.ChatSid
                        };
                        var token = await _roomService.GenerateRoomToken(deposition.Room.Name, user, role, identity, chatInfo);
                        if (token.IsFailed)
                            return token.ToResult<JoinDepositionDto>();

                        joinDepositionInfo = SetJoinDepositionInfo(token.Value, deposition, joinDepositionInfo, false);
                    }
                }
                else
                {
                    await StartDepositionRoom(deposition.PreRoom, false);

                    var preRoomToken = await _roomService.GenerateRoomToken(deposition.PreRoom.Name, user, role, identity);
                    if (preRoomToken.IsFailed)
                        return preRoomToken.ToResult<JoinDepositionDto>();

                    joinDepositionInfo = SetJoinDepositionInfo(preRoomToken.Value, deposition, joinDepositionInfo, true);
                }
            }

            if (currentParticipant == null && !user.IsAdmin)
                return Result.Fail(new InvalidInputError($"User is neither a Participant for this Deposition nor an Admin"));

            return Result.Ok(joinDepositionInfo);
        }

        private async Task StartDepositionRoom(Room room, bool configureCallBacks)
        {
            // TODO: Add distributed lock when our infra allows it
            if (room.Status == RoomStatus.Created)
            {
                await _roomService.StartRoom(room, configureCallBacks);
            }
        }

        private JoinDepositionDto SetJoinDepositionInfo(string token, Deposition deposition, JoinDepositionDto joinDepositionInfo, bool shouldSendToPreDepo)
        {
            joinDepositionInfo.Token = token;
            joinDepositionInfo.ShouldSendToPreDepo = shouldSendToPreDepo;
            joinDepositionInfo.TimeZone = Enum.GetValues(typeof(USTimeZone)).Cast<USTimeZone>().FirstOrDefault(x => x.GetDescription() == deposition.TimeZone).ToString();
            joinDepositionInfo.IsOnTheRecord = deposition.IsOnTheRecord;
            joinDepositionInfo.IsSharing = deposition.SharingDocumentId.HasValue;
            joinDepositionInfo.Participants = deposition.Participants.Where(x => x.HasJoined == true).Select(p => _participantMapper.ToDto(p)).ToList();
            joinDepositionInfo.StartDate = deposition.StartDate;
            joinDepositionInfo.JobNumber = deposition.Job;

            return joinDepositionInfo;
        }

        private async Task SendNotification(Deposition deposition)
        {
            var notificationDto = new NotificationDto
            {
                EntityType = NotificationEntity.Deposition,
                Action = NotificationAction.Start
            };

            await _signalRNotificationManager.SendNotificationToDepositionMembers(deposition.Id, notificationDto);
        }

        private async Task UpdateCurrentParticipant(Participant currentParticipant)
        {
            currentParticipant.HasJoined = true;
            await _participantRepository.Update(currentParticipant);
        }

        private async Task SendJoinDepositionEmailNotification(Deposition deposition)
        {
            var tasks = deposition.Participants.Where(p => !string.IsNullOrWhiteSpace(p.Email)).Select(participant =>
            {
                var template = GetJoinDepositionEmailTemplate(deposition, participant);
                return _awsEmailService.SendRawEmailNotification(template);
            });

            await Task.WhenAll(tasks);
        }

        private async Task SendJoinDepositionEmailNotification(Deposition deposition, Participant participant)
        {
            var template = GetJoinDepositionEmailTemplate(deposition, participant);
            await _awsEmailService.SendRawEmailNotification(template);
        }

        private async Task SendCancelDepositionEmailNotification(Deposition deposition, Participant participant)
        {
            var template = GetCancelDepositionEmailTemplate(deposition, participant);
            await _awsEmailService.SendRawEmailNotification(template);
        }

        private async Task SendReSheduleDepositionEmailNotification(Deposition deposition, Participant participant, DateTime oldStartDate, string oldTimeZone)
        {
            var template = GetReScheduleDepositionEmailTemplate(deposition, participant, oldStartDate, oldTimeZone);
            await _awsEmailService.SendRawEmailNotification(template);
        }

        private EmailTemplateInfo GetJoinDepositionEmailTemplate(Deposition deposition, Participant participant)
        {
            var template = new EmailTemplateInfo
            {
                EmailTo = new List<string> { participant.Email },
                TemplateData = new Dictionary<string, string>
                            {
                                { "dateAndTime", deposition.StartDate.GetFormattedDateTime(deposition.TimeZone) },
                                { "name", participant.Name ?? string.Empty },
                                { "case", GetDescriptionCase(deposition) },
                                { "imageUrl",  GetImageUrl(_emailConfiguration.LogoImageName) },
                                { "calendar", GetImageUrl(_emailConfiguration.CalendarImageName) },
                                { "depositionJoinLink", $"{_emailConfiguration.PreDepositionLink}{deposition.Id}"}
                            },
                TemplateName = _emailConfiguration.JoinDepositionTemplate,
                Calendar = CreateCalendar(deposition, CalendarAction.Add.GetDescription()),
                AddiotionalText = $"You can join by clicking the link: {_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Subject = $"Invitation: Remote Legal - {GetSubject(deposition)}"
            };

            return template;
        }

        private EmailTemplateInfo GetCancelDepositionEmailTemplate(Deposition deposition, Participant participant)
        {
            var template = new EmailTemplateInfo
            {
                EmailTo = new List<string> { participant.Email },
                TemplateData = new Dictionary<string, string>
                            {
                                { "start-date", deposition.StartDate.GetFormattedDateTime(deposition.TimeZone) },
                                { "user-name", participant.Name },
                                { "case-name", GetDescriptionCase(deposition) },
                                { "images-url",  _emailConfiguration.ImagesUrl },
                                { "logo", GetImageUrl(_emailConfiguration.LogoImageName) }
                            },
                TemplateName = ApplicationConstants.CancelDepositionEmailTemplate,
                AddiotionalText = string.Empty,
                Calendar = CreateCalendar(deposition, CalendarAction.Cancel.GetDescription()),
                Subject = $"Cancellation: Remote Legal - {GetSubject(deposition)}"
            };

            return template;
        }

        private EmailTemplateInfo GetReScheduleDepositionEmailTemplate(Deposition deposition, Participant participant, DateTime oldStartDate, string oldTimeZone)
        {
            var template = new EmailTemplateInfo
            {
                EmailTo = new List<string> { participant.Email },
                TemplateData = new Dictionary<string, string>
                            {
                                { "old-start-date", oldStartDate.GetFormattedDateTime(oldTimeZone) },
                                { "start-date", deposition.StartDate.GetFormattedDateTime(deposition.TimeZone) },
                                { "user-name", participant.Name ?? string.Empty },
                                { "case-name", GetDescriptionCase(deposition) },
                                { "images-url",  _emailConfiguration.ImagesUrl },
                                { "logo", GetImageUrl(_emailConfiguration.LogoImageName) },
                                { "deposition-join-link", $"{_emailConfiguration.PreDepositionLink}{deposition.Id}"}
                            },
                TemplateName = ApplicationConstants.ReScheduleDepositionEmailTemplate,
                AddiotionalText = $"You can join by clicking the link: {_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Calendar = CreateCalendar(deposition, CalendarAction.Update.GetDescription()),
                Subject = $"Invitation update: Remote Legal - {GetSubject(deposition)}"
            };

            return template;
        }

        private string GetSubject(Deposition deposition)
        {
            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var subject = $"{deposition.Case.Name} - {deposition.StartDate.GetFormattedDateTime(deposition.TimeZone)}";

            if (!string.IsNullOrEmpty(witness?.Name))
                subject = $"{witness.Name} - {deposition.Case.Name} - {deposition.StartDate.GetFormattedDateTime(deposition.TimeZone)}";

            return subject;
        }

        private string GetDescriptionCase(Deposition deposition)
        {
            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var caseName = $"<b>{deposition.Case.Name}</b>";

            if (!string.IsNullOrEmpty(witness?.Name))
                caseName = $"<b>{witness.Name}</b> in the case of <b>{caseName}</b>";

            return caseName;
        }

        private string GetImageUrl(string name)
        {
            return $"{_emailConfiguration.ImagesUrl}{name}";
        }

        private Calendar CreateCalendar(Deposition deposition, string method = "REQUEST")
        {
            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var strWitness = !string.IsNullOrWhiteSpace(witness?.Name) ? $"{witness.Name} - {deposition.Case.Name} " : deposition.Case.Name;
            var calendar = new Calendar
            {
                Method = method
            };

            var icalEvent = new CalendarEvent
            {
                Uid = deposition.Id.ToString(),
                Summary = $"Invitation: Remote Legal - {strWitness}",
                Description = $"{strWitness}{Environment.NewLine}{_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Start = new CalDateTime(deposition.StartDate.GetConvertedTime(deposition.TimeZone), deposition.TimeZone),
                End = deposition.EndDate.HasValue ? new CalDateTime(deposition.EndDate.Value.GetConvertedTime(deposition.TimeZone), deposition.TimeZone) : null,
                Location = $"{_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Organizer = new Organizer(_emailConfiguration.EmailNotification)
            };

            calendar.Events.Add(icalEvent);

            return calendar;
        }
    }
}
