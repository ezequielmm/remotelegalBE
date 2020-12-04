﻿using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Repositories.Interfaces
{
    public interface IUserResourceRoleRepository
    {
        Task<UserResourceRole> Create(UserResourceRole entity);
        Task<bool> CheckUserHasPermissionForAction(Guid userId, ResourceType resourceType, Guid resourceId, ResourceAction resourceAction);
    }
}