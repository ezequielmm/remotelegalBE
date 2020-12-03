using FluentResults;
using PrecisionReporters.Platform.Data.Enums;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<Result> AddUserRole(Guid userId, Guid resourceId, ResourceType resourceType, RoleName roleName);
        Task<bool> CheckUserHasPermissionForAction(string userEmail, ResourceType resourceType, Guid resourceId, ResourceAction resourceAction);
    }
}
