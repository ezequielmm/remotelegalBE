using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDepositionService
    {
        Task<List<Deposition>> GetDepositions(Expression<Func<Deposition, bool>> filter = null, string[] include = null);
        Task<Result<Deposition>> GetDepositionById(Guid id);
        Task<Result<Deposition>> GetDepositionByIdWithDocumentUsers(Guid id);
        Task<Result<Deposition>> GenerateScheduledDeposition(Guid caseId, Deposition deposition, List<Document> uploadedDocuments, User addedBy);
        Task<List<Deposition>> GetDepositionsByStatus(DepositionStatus? status, DepositionSortField? sortedField, SortDirection? sortDirection, string userEmail);
        Task<Result<JoinDepositionDto>> JoinDeposition(Guid id, string identity);
        Task<Result<Deposition>> EndDeposition(Guid id);
        Task<Result<Participant>> GetDepositionParticipantByEmail(Guid id, string participantEmail);
        Task<Result<List<DepositionEvent>>> GetDepositionEvents(Guid id);
        Task<Result<Deposition>> AddDepositionEvent(Guid id, DepositionEvent depositionEvent, string userEmail);
        Task<Result<DepositionEvent>> GoOnTheRecord(Guid id, bool onTheRecord, string userEmail);
        Task<Result<Deposition>> Update(Deposition deposition);
        Task<Result<Document>> GetSharedDocument(Guid id);
        Task<Result<string>> JoinBreakRoom(Guid depositionId, Guid breakRoomId);
        Task<Result> LeaveBreakRoom(Guid depositionId, Guid breakRoomId);
        Task<Result<BreakRoom>> LockBreakRoom(Guid depositionId, Guid breakRoomId, bool lockRoom);
        Task<Result<List<BreakRoom>>> GetDepositionBreakRooms(Guid id);
        Task<Result<(Participant, bool)>> CheckParticipant(Guid id, string emailAddress);
        Task<Result<GuestToken>> JoinGuestParticipant(Guid depositionId, Participant guest);
        Task<Result<Guid>> AddParticipant(Guid depositionId, Participant participant);
        Task<Result<Deposition>> ClearDepositionDocumentSharingId(Guid depositionId);
        Task<Result<Deposition>> GetDepositionByRoomId(Guid roomId);
        Task<Result<DepositionVideoDto>> GetDepositionVideoInformation(Guid depositionId);
        Task<Result<List<Participant>>> GetDepositionParticipants(Guid depositionId,
            ParticipantSortField sortedField = ParticipantSortField.Role,
            SortDirection sortDirection = SortDirection.Descend);
        Task<Result<Document>> GetDepositionCaption(Guid id);
        Task<Result<Participant>> AddParticipantToExistingDeposition(Guid id, Participant participant);
    }
}