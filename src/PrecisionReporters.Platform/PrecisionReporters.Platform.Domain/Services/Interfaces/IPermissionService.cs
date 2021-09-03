using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<Result> AddUserRole(Guid userId, Guid resourceId, ResourceType resourceType, RoleName roleName);
        Task<bool> CheckUserHasPermissionForAction(string userEmail, ResourceType resourceType, Guid resourceId, ResourceAction resourceAction);
        Task<Result<List<ResourceAction>>> GetDepositionUserPermissions(Participant participant, Guid depositionId, bool isAdmin = false);
        Task AddParticipantPermissions(Participant participant);
        Task RemoveParticipantPermissions(Guid depositionId, Participant participant);
        Task AddRolesToParticipants(Participant participant, Guid depositionId);

    }
}
