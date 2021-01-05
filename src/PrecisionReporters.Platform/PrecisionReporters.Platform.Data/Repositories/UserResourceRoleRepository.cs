using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class UserResourceRoleRepository : IUserResourceRoleRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserResourceRoleRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CheckUserHasPermissionForAction(Guid userId, ResourceType resourceType, Guid resourceId, ResourceAction resourceAction)
        {
            return await _dbContext.Set<Role>().AsNoTracking()
                .Join(_dbContext.Set<UserResourceRole>().AsNoTracking(), x => x.Id, x => x.RoleId, (r, urr) => new { Role = r, UserResourceRole = urr })
                .Join(_dbContext.Set<RolePermission>().AsNoTracking(), x => x.Role.Id, x => x.RoleId, (urr, rp) => new { RolePermission = rp, UserResourceRole = urr.UserResourceRole })
                .AnyAsync(x => x.UserResourceRole.UserId == userId && x.UserResourceRole.ResourceId == resourceId && x.UserResourceRole.ResourceType == resourceType && x.RolePermission.Action == resourceAction);
        }

        public async Task<List<ResourceAction>> GetUserActionsForResource(Guid userId, ResourceType resourceType, Guid resourceId)
        {
            return await _dbContext.Set<Role>().AsNoTracking()
                .Join(_dbContext.Set<UserResourceRole>().AsNoTracking(), x => x.Id, x => x.RoleId, (r, urr) => new { Role = r, UserResourceRole = urr })
                .Join(_dbContext.Set<RolePermission>().AsNoTracking(), x => x.Role.Id, x => x.RoleId, (urr, rp) => new { RolePermission = rp, UserResourceRole = urr.UserResourceRole })
                .Where(x => x.UserResourceRole.UserId == userId && x.UserResourceRole.ResourceId == resourceId && x.UserResourceRole.ResourceType == resourceType)
                .Select(x => x.RolePermission.Action).ToListAsync();
        }

        public async Task<UserResourceRole> Create(UserResourceRole entity)
        {
            await _dbContext.Set<UserResourceRole>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
    }
}
