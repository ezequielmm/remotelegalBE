using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Repositories.Interfaces
{
    public interface IUserResourceRoleRepository
    {
        Task<UserResourceRole> Create(UserResourceRole entity);
        Task<bool> CheckUserHasPermissionForAction(Guid userId, ResourceType resourceType, Guid resourceId, ResourceAction resourceAction);
        Task<List<ResourceAction>> GetUserActionsForResource(Guid userId, ResourceType resourceType, Guid resourceId);
        Task Remove(UserResourceRole entity);
        Task<UserResourceRole> GetFirstOrDefaultByFilter(Expression<Func<UserResourceRole, bool>> filter = null, string[] include = null);
    }
}
